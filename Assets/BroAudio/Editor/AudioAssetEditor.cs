using System;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Ami.Extension.Reflection;
using System.Reflection;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.Extension.EditorScriptingExtension;
using System.Collections.Generic;
using System.IO;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(AudioAsset), true)]
    public class AudioAssetEditor : UnityEditor.Editor
    {
        private ReorderableList _entitiesList;

        private readonly List<AudioEntityEditor> _entityEditors = new List<AudioEntityEditor>();

        public Instruction CurrInstruction { get; private set; }
        public IAudioAsset Asset { get; private set; }

        public void AddEntitiesListener()
        {
            AudioEntityEditor.OnDuplicateEntity += OnDuplicateSelectedEntity;
            AudioEntityEditor.OnRemoveEntity += OnRemoveSelectedEntity;
            AudioEntityEditor.OnExpandAll += SetAllElementsExpanded;
        }

        public void RemoveEntitiesListener()
        {
            AudioEntityEditor.OnDuplicateEntity -= OnDuplicateSelectedEntity;
            AudioEntityEditor.OnRemoveEntity -= OnRemoveSelectedEntity;
            AudioEntityEditor.OnExpandAll -= SetAllElementsExpanded;
        }

        private void OnDestroy()
        {
            RemoveEntitiesListener();
        }

        public void Init()
        {
            Asset = target as IAudioAsset;
            InitReorderableList(); 
        }

        private void OnRemoveSelectedEntity(AudioEntityEditor editor)
        {
            OnRemoveSelectedEntity(false, editor);
        }

        private void OnRemoveSelectedEntity(bool showDialog, AudioEntityEditor editor)
        {
            if(showDialog)
            {
                if(!EditorUtility.DisplayDialog("Remove Entity", $"Do you want to remove [{editor.target.name}]?", "Yes", "No"))
                {
                    return;
                }
            }

            _entityEditors.Remove(editor);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(editor.target));
            DestroyImmediate(editor.target, true);
        }

        private void OnDuplicateSelectedEntity(AudioEntityEditor editor)
        {
            if (editor != null && editor.target is AudioEntity entity)
            {
                var newEntity = Instantiate(entity);
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(entity));
                newEntity.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(newEntity, path);
                var newEditor = CreateEditor(newEntity, typeof(AudioEntityEditor)) as AudioEntityEditor;
                _entityEditors.Add(newEditor);
                _entitiesList.index = _entityEditors.Count - 1;
                newEditor.IsExpanded = true;
            }
        }

        public void SetData(string guid, string assetName)
        {
            string assetGUIDPropertyPath = GetFieldName(nameof(IAudioAsset.AssetGUID));
            serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

            string assetNamePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AssetName));
            serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private string GetAudioAssetDirectoryPath()
        {
            if (Asset != null)
            {
                var path = Path.Combine(AssetOutputPath, Asset.AssetName).Replace('\\', '/');

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                return path;
            }

            return AssetOutputPath;
        }

        private void InitReorderableList()
        {
            _entityEditors.Clear();

            if (Asset != null)
            {
                List<AudioEntity> entities = new List<AudioEntity>();
                GetAudioEntities(entities, Asset as AudioAsset);

                foreach (var entity in entities)
                {
                    AudioEntityEditor entityEditor = (AudioEntityEditor)CreateEditor(entity, typeof(AudioEntityEditor));
                    _entityEditors.Add(entityEditor);
                }

                _entitiesList = new ReorderableList(_entityEditors, typeof(AudioEntityEditor),
                    draggable: false, displayHeader: false, displayAddButton: true, displayRemoveButton: true)
                {
                    onAddCallback = OnAdd,
                    onRemoveCallback = OnRemove,
                    drawElementCallback = OnDrawElement,
                    elementHeightCallback = OnGetPropertyHeight
                };
            }

            void OnAdd(ReorderableList list)
            {
                BroAudioType audioType = BroAudioType.None;

                if (list.count > 0)
                {
                    var lastEditor = _entityEditors[list.count - 1];
                    if (lastEditor.target is AudioEntity entity)
                    {
                        audioType = entity.AudioType;
                    }
                }

                CreateNewEntity("New Sound", audioType);
            }

            void OnRemove(ReorderableList list)
            {
                if (list.index >= 0 && list.index < _entityEditors.Count)
                {
                    OnRemoveSelectedEntity(true, _entityEditors[list.index]);
                }
            }

            void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (_entityEditors[index] is AudioEntityEditor editor)
                {
                    if (!editor.IsExpanded && EventExtension.IsRightClick(rect))
                    {
                        _entitiesList.index = index;
                        _entitiesList.GrabKeyboardFocus();
                        // the element background doesn't repaint right away, so we delay the dropdown to the next drawing process 
                        EditorApplication.delayCall += () => editor.OnOpenOptionMenu();
                        EditorWindow.focusedWindow.Repaint();
                    }

                    HandleKeyboardShortcuts();

                    editor.DrawGUI(rect);
                }
            }

            float OnGetPropertyHeight(int index)
            {
                if (_entityEditors[index] is AudioEntityEditor editor)
                {
                    return editor.GetHeight();
                }

                return EditorGUIUtility.singleLineHeight;
            }
        }

        private void HandleKeyboardShortcuts()
        {
            var current = Event.current;
            if(current.type != EventType.KeyDown)
            {
                return;
            }

            bool isCtrl = current.control || current.modifiers == EventModifiers.Control;
            if (_entitiesList.index >= 0 && _entitiesList.index < _entityEditors.Count && _entityEditors[_entitiesList.index] is AudioEntityEditor selected)
            {
                if (isCtrl && current.keyCode == KeyCode.D)
                {
                    OnDuplicateSelectedEntity(selected);
                    current.Use();
                }
                else if (current.keyCode == KeyCode.Delete)
                {
                    if (!selected.IsExpanded)
                    {
                        OnRemoveSelectedEntity(true, selected);
                        current.Use();
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Open " + BroName.MenuItem_LibraryManager))
            {
                LibraryManagerWindow window = LibraryManagerWindow.ShowWindow();
                if(Asset == null)
                {
                    Asset = target as IAudioAsset;
                }
                window.SelectAsset(Asset as AudioAsset);
                Init();
            }
        }

        public void DrawEntitiesList(out float height)
        {
            _entitiesList.DoLayoutList();
            height = _entitiesList.GetHeight();
        }

        private void SetAllElementsExpanded(bool isExpanded)
        {
            foreach (var editor in _entityEditors)
            {
                editor.IsExpanded = isExpanded;
            }
        }

        public void SetAssetName(string newName)
        {
            var asset = Asset as AudioAsset;
            string path = AssetDatabase.GetAssetPath(asset);
            AssetDatabase.RenameAsset(path, newName);

            serializedObject.Update();
            serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AssetName))).stringValue = newName;
            serializedObject.ApplyModifiedProperties();
        }

        public (AudioEntity entity, AudioEntityEditor editor) CreateNewEntity(string name, BroAudioType audioType = BroAudioType.None)
        {
            var path = Path.Combine(GetAudioAssetDirectoryPath(), name + ".asset").Replace('\\', '/');
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            name = Path.GetFileNameWithoutExtension(path);

            var newEntity = AudioEntity.CreateNewInstance(Asset as AudioAsset, name, audioType);

            AssetDatabase.CreateAsset(newEntity, path);

            var editor = (AudioEntityEditor)CreateEditor(newEntity, typeof(AudioEntityEditor));
            _entityEditors.Add(editor);

            return (newEntity, editor);
        }

        public void SetClipList(SerializedProperty clipListProp, int index, AudioClip clip)
        {
            clipListProp.InsertArrayElementAtIndex(index);
            SerializedProperty elementProp = clipListProp.GetArrayElementAtIndex(index);
            elementProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip).objectReferenceValue = clip;
            elementProp.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = AudioConstant.FullVolume;
        }

        public void SelectEntity(AudioEntity entity, out float entityVerticalPos)
        {
            entityVerticalPos = 0f;
            foreach (var editor in _entityEditors)
            {
                var isTarget = editor.target == entity;
                editor.IsExpanded = isTarget;
                if (isTarget)
                {
                    _entitiesList.index = _entityEditors.IndexOf(editor);
                    entityVerticalPos = _entitiesList.index * _entitiesList.elementHeight;
                }
            }
        }

        public void Verify()
        {
            if(VerifyAsset())
            {
                CurrInstruction = default;
            }
        }

        private bool VerifyAsset()
        {
            if (IsInvalidName(Asset.AssetName, out ValidationErrorCode code))
            {
                CurrInstruction = code switch 
                {
                    ValidationErrorCode.IsNullOrEmpty => Instruction.AssetNaming_IsNullOrEmpty,
                    ValidationErrorCode.StartWithNumber => Instruction.AssetNaming_StartWithNumber,
                    ValidationErrorCode.ContainsInvalidWord => Instruction.AssetNaming_ContainsInvalidWords,
                    ValidationErrorCode.ContainsWhiteSpace => Instruction.AssetNaming_ContainsWhiteSpace,
                    _ => default,
                };
                return false;
            }
            return true;
        }
    }
}