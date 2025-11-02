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
        private ReorderableList _list;
        private ReorderableList list
        {
            get
            {
                if (_list != null)
                {
                    return _list;
                }

                RebuildList();

                return _list;
            }
        }

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

            if (_list != null)
            {
                _list.list.Remove(editor);
            }

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
                var newEditor = AddOrMoveToEnd(newEntity);
                if (_list != null)
                {
                    _list.index = _list.count - 1;
                }
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

        private AudioEntityEditor GetAudioEntityEditor(int index)
        {
            if (list != null && index >= 0 && index < _list.count)
            {
                return list.list[index] as AudioEntityEditor;
            }

            return null;
        }

        public void RebuildList()
        {
            var audioAsset = Asset as AudioAsset;

            if (audioAsset == null)
            {
                _list = new ReorderableList(new List<AudioEntityEditor>(), typeof(AudioEntityEditor));
                return;
            }

            List<AudioEntity> entities = new List<AudioEntity>();
            GetAudioEntities(entities, Asset as AudioAsset);

            List<AudioEntityEditor> editors = new List<AudioEntityEditor>(entities.Count);

            if (_list != null) // Some logic to retain the current sorting until we've fully closed
            {
                foreach (var rawEditor in _list.list)
                {
                    if (rawEditor is AudioEntityEditor editor)
                    {
                        var entity = editor.target as AudioEntity;
                        var audioEntityListIndex = entities.IndexOf(entity);

                        if (audioEntityListIndex == -1)
                        {
                            continue; // not included in the new list
                        }

                        // remove it from our entities list
                        entities.RemoveAt(audioEntityListIndex);

                        // add it to the new list
                        editors.Add(editor);
                    }
                }
            }

            foreach (var entity in entities)
            {
                AudioEntityEditor entityEditor = (AudioEntityEditor)CreateEditor(entity, typeof(AudioEntityEditor));
                editors.Add(entityEditor);
            }

            _list = new ReorderableList(editors, typeof(AudioEntityEditor),
                draggable: false, displayHeader: false, displayAddButton: true, displayRemoveButton: true)
            {
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                drawElementCallback = OnDrawElement,
                elementHeightCallback = OnGetPropertyHeight
            };

            void OnAdd(ReorderableList list)
            {
                BroAudioType audioType = BroAudioType.None;

                if (list.count > 0)
                {
                    var lastEditor = GetAudioEntityEditor(list.count - 1);
                    if (lastEditor.target is AudioEntity entity)
                    {
                        audioType = entity.AudioType;
                    }
                }

                CreateNewEntity("New Sound", audioType);
            }

            void OnRemove(ReorderableList list)
            {
                var editor = GetAudioEntityEditor(list.index);
                if (editor != null)
                {
                    OnRemoveSelectedEntity(true, editor);
                }
            }

            void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                var editor = GetAudioEntityEditor(index);
                if (editor != null)
                {
                    if (!editor.IsExpanded && EventExtension.IsRightClick(rect))
                    {
                        list.index = index;
                        list.GrabKeyboardFocus();
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
                var editor = GetAudioEntityEditor(index);

                if (editor != null)
                {
                    return editor.GetHeight();
                }

                return EditorGUIUtility.singleLineHeight;
            }
        }

        public void ClearList()
        {
            _list = null;
        }

        private void HandleKeyboardShortcuts()
        {
            var current = Event.current;
            if(current.type != EventType.KeyDown)
            {
                return;
            }

            bool isCtrl = current.control || current.modifiers == EventModifiers.Control;
            if (_list != null && GetAudioEntityEditor(_list.index) is AudioEntityEditor selected)
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
            list.DoLayoutList();
            height = list.GetHeight();
        }

        private void SetAllElementsExpanded(bool isExpanded)
        {
            foreach (var editor in list.list)
            {
                if (editor is AudioEntityEditor entityEditor)
                {
                    entityEditor.IsExpanded = isExpanded;
                }
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
            var editor = AddOrMoveToEnd(newEntity);
            return (newEntity, editor);
        }

        private AudioEntityEditor AddOrMoveToEnd(AudioEntity entity)
        {
            if (_list == null)
            {
                return CreateEditor(entity, typeof(AudioEntityEditor)) as AudioEntityEditor; // just so we don't error
            }

            AudioEntityEditor editor = null;

            for (int i = _list.count - 1; i >= 0; i--)
            {
                if (_list.list[i] is AudioEntityEditor listEditorItem && listEditorItem.target == entity)
                {
                    editor = listEditorItem;

                    if (i != _list.count - 1)
                    {
                        // swap the item with the last item
                        (_list.list[i], _list.list[_list.count - 1]) = (_list.list[_list.count - 1], _list.list[i]);
                    }

                    break;
                }
            }

            if (editor == null)
            {
                editor = CreateEditor(entity, typeof(AudioEntityEditor)) as AudioEntityEditor;
                _list.list.Add(editor);
            }

            return editor;
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
            foreach (var editor in list.list)
            {
                if (editor is AudioEntityEditor entityEditor)
                {
                    var isTarget = entityEditor.target == entity;
                    entityEditor.IsExpanded = isTarget;
                    if (isTarget)
                    {
                        list.index = list.list.IndexOf(editor);
                        entityVerticalPos = list.index * list.elementHeight;
                    }
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