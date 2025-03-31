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

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(AudioAsset), true)]
    public class AudioAssetEditor : UnityEditor.Editor
    {
        private ReorderableList _entitiesList = null;
        private IUniqueIDGenerator _idGenerator = null;
        private Action _onDropDownMenu;
        private Action<SerializedProperty, GenericMenu, Event> _setupPropertyCopyPasteDelegate;

        public Instruction CurrInstruction { get; private set; }
        public IAudioAsset Asset { get; private set; }
        private Action<SerializedProperty, GenericMenu, Event> SetupPropertycopyPasteDelegate
        {
            get
            {
                if (_setupPropertyCopyPasteDelegate == null)
                {
                    Type type = ClassReflectionHelper.GetUnityEditorClass("ClipboardContextMenu");
                    MethodInfo method = type.GetMethod("SetupPropertyCopyPaste", BindingFlags.NonPublic | BindingFlags.Static);
                    _setupPropertyCopyPasteDelegate = (Action<SerializedProperty, GenericMenu, Event>)Delegate.CreateDelegate(typeof(Action<SerializedProperty, GenericMenu, Event>), method);
                }
                return _setupPropertyCopyPasteDelegate;
            }
        }

        public void AddEntitiesListener()
        {
            AudioEntityPropertyDrawer.OnDulicateEntity += OnDuplicateSelectedEntity;
            AudioEntityPropertyDrawer.OnRemoveEntity += OnRemoveSelectedEntity;
            AudioEntityPropertyDrawer.OnExpandAll += SetAllElementsExpanded;
        }

        public void RemoveEntitiesListener()
        {
            AudioEntityPropertyDrawer.OnDulicateEntity -= OnDuplicateSelectedEntity;
            AudioEntityPropertyDrawer.OnRemoveEntity -= OnRemoveSelectedEntity;
            AudioEntityPropertyDrawer.OnExpandAll -= SetAllElementsExpanded;
        }

        private void OnDestroy()
        {
            RemoveEntitiesListener();
        }

        public void Init(IUniqueIDGenerator idGenerator)
        {
            Asset = target as IAudioAsset;
            _idGenerator = idGenerator;
            InitReorderableList(); 
        }

        private void OnRemoveSelectedEntity()
        {
            OnRemoveSelectedEntity(false);
        }

        private void OnRemoveSelectedEntity(bool showDialog)
        {
            if(showDialog)
            {
                var selectedProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(_entitiesList.index);
                string selectedEntityName = selectedProp.FindBackingFieldProperty(nameof(AudioEntity.Name)).stringValue;
                if(!EditorUtility.DisplayDialog("Remove Entity", $"Do you want to remove [{selectedEntityName}]?", "Yes", "No"))
                {
                    return;
                }
            }
            ReorderableList.defaultBehaviours.DoRemoveButton(_entitiesList);
            serializedObject.ApplyModifiedProperties();
        }

        private void OnDuplicateSelectedEntity()
        {
            var listProp = _entitiesList.serializedProperty;
            int sourceIndex = _entitiesList.index;
            listProp.MoveArrayElement(sourceIndex, _entitiesList.count - 1);
            ReorderableList.defaultBehaviours.DoAddButton(_entitiesList);
            listProp.MoveArrayElement(_entitiesList.count - 1, sourceIndex);
            listProp.MoveArrayElement(_entitiesList.count - 1, sourceIndex); // move both of them to the original pos

            var sourceProp = listProp.GetArrayElementAtIndex(sourceIndex);
            string sourceEntityName = sourceProp.FindBackingFieldProperty(nameof(AudioEntity.Name)).stringValue;

            var newProp = listProp.GetArrayElementAtIndex(sourceIndex + 1);
            var newEntityIdProp = newProp.FindBackingFieldProperty(nameof(AudioEntity.ID));
            newEntityIdProp.intValue = _idGenerator.GetSimpleUniqueID(Utility.GetAudioType(newEntityIdProp.intValue));

            var newEntityNameProp = newProp.FindBackingFieldProperty(nameof(AudioEntity.Name));
            int newNameIndex = 1;
            if (sourceEntityName[sourceEntityName.Length - 1] == ')')
            {
                int leftParenthesisIndex = sourceEntityName.IndexOf('(');
                string nameIndexString = sourceEntityName.Substring(leftParenthesisIndex + 1, sourceEntityName.Length - leftParenthesisIndex - 2);
                newNameIndex = int.Parse(nameIndexString) + 1;
                sourceEntityName = sourceEntityName.Remove(leftParenthesisIndex - 1); // with space
            }
            newEntityNameProp.stringValue = sourceEntityName + $" ({newNameIndex})";

            RecoverShiftedExpandedStates(listProp, sourceIndex);
            _entitiesList.index = sourceIndex + 1;
            serializedObject.ApplyModifiedProperties();
        }

        private void RecoverShiftedExpandedStates(SerializedProperty listProp, int sourceIndex)
        {
            SerializedProperty lastProp = null;
            for (int i = _entitiesList.count - 1; i >= sourceIndex; i--)
            {
                var currentProp = listProp.GetArrayElementAtIndex(i);
                if (lastProp != null)
                {
                    lastProp.isExpanded = currentProp.isExpanded;
                }
                lastProp = currentProp;
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

        private void InitReorderableList()
        {
            if (Asset != null)
            {
                _entitiesList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(AudioAsset.Entities)), true,false,true,true)
                {
                    onAddCallback = OnAdd,
                    onRemoveCallback = OnRemove,
                    drawElementCallback = OnDrawElement,
                    elementHeightCallback = OnGetPropertyHeight,
                    onReorderCallback = OnReorder,
                };
            }

            void OnAdd(ReorderableList list)
            {
                BroAudioType audioType = BroAudioType.None;
                if (list.count > 0)
                {
                    var lastElementProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                    var lastElementID = lastElementProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID))).intValue;
                    audioType = Utility.GetAudioType(lastElementID); 
                }
                ReorderableList.defaultBehaviours.DoAddButton(list);
                SerializedProperty newEntity = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                ResetEntitySerializedProperties(newEntity);
                AssignID(newEntity, audioType);

                serializedObject.ApplyModifiedProperties();
            }

            void OnRemove(ReorderableList list)
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                serializedObject.ApplyModifiedProperties();
            }

            void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                SerializedProperty elementProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(index);

                if (!elementProp.isExpanded && EventExtension.IsRightClick(rect))
                {
                    _entitiesList.index = index;
                    _entitiesList.GrabKeyboardFocus();
                    string entityName = elementProp.FindBackingFieldProperty(nameof(AudioEntity.Name)).stringValue;
                    Rect dropDownRect = new Rect(rect) { x = Event.current.mousePosition.x };
                    // the element background doesn't repaint right away, so we delay the dropdown to the next drawing process 
                    _onDropDownMenu = () => OnDropDwonRightClickMenu(dropDownRect, elementProp);
                    EditorWindow.focusedWindow.Repaint();
                }

                HandleKeyboardShortcuts();

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, elementProp);
                if(EditorGUI.EndChangeCheck())
                {
                    elementProp.serializedObject.ApplyModifiedProperties();
                }
            }

            void OnDropDwonRightClickMenu(Rect rect, SerializedProperty property)
            {
                string buffer = EditorGUIUtility.systemCopyBuffer;
                if(!string.IsNullOrEmpty(buffer) && buffer.StartsWith("GenericPropertyJSON:"))
                {
                    string targetString = "\"name\":\"<ID>k__BackingField\",\"type\":0,\"val\":";
                    int idStartIndex = buffer.IndexOf(targetString);
                    if(idStartIndex > 0)
                    {
                        int valStart = idStartIndex + targetString.Length;
                        int valEnd = buffer.IndexOf('}', valStart);
                        int id = property.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
                        buffer = buffer.Remove(valStart, valEnd - valStart).Insert(valStart, id.ToString());
                        EditorGUIUtility.systemCopyBuffer = buffer;
                    }
                }

                // similar to the default context menu but with more customized features
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent($"Duplicate ^D"), false, OnDuplicateSelectedEntity);
                menu.AddItem(new GUIContent($"Remove _DELETE"), false, OnRemoveSelectedEntity);
                SetupPropertycopyPasteDelegate?.Invoke(property, menu, Event.current);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Copy Property Path"), false, OnCopyPropertyPath);

                menu.DropDown(rect);
            }

            void OnReorder(ReorderableList list)
            {
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            }

            float OnGetPropertyHeight(int index)
            {
                return EditorGUI.GetPropertyHeight(_entitiesList.serializedProperty.GetArrayElementAtIndex(index));
            }
        }

        private void OnCopyPropertyPath()
        {
            EditorGUIUtility.systemCopyBuffer = _entitiesList.serializedProperty.GetArrayElementAtIndex(_entitiesList.index).propertyPath;
        }

        private void HandleKeyboardShortcuts()
        {
            var current = Event.current;
            if(current.type != EventType.KeyDown)
            {
                return;
            }

            bool isCtrl = current.control || current.modifiers == EventModifiers.Control;

            if (isCtrl && current.keyCode == KeyCode.D)
            {
                OnDuplicateSelectedEntity();
                current.Use();
            }
            else if (current.keyCode == KeyCode.Delete)
            {
                var selectedProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(_entitiesList.index);
                if(!selectedProp.isExpanded)
                {
                    OnRemoveSelectedEntity(true);
                    current.Use();
                }
            }
        }

        private void AssignID(SerializedProperty entityProp, BroAudioType audioType)
        {
            AssignID(_idGenerator.GetSimpleUniqueID(audioType), entityProp);
        }

        private void AssignID(int id, SerializedProperty entityProp)
        {
            var idProp = entityProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID)));
            idProp.intValue = id;
            entityProp.serializedObject.ApplyModifiedProperties();
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
                window.SelectAsset(Asset.AssetGUID);
                Init(window.IDGenerator);
            }
        }

        public void DrawEntitiesList(out float height)
        {        
            if(_onDropDownMenu != null)
            {
                _onDropDownMenu.Invoke();
                _onDropDownMenu = null;
            }

            _entitiesList.DoLayoutList();
            height = _entitiesList.GetHeight();
        }

        private void SetAllElementsExpanded(bool isExpanded)
        {
            for(int i = 0; i < _entitiesList.count; i++)
            {
                SerializedProperty elementProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(i);
                elementProp.isExpanded = isExpanded;
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

        public SerializedProperty CreateNewEntity()
        {
            ReorderableList.defaultBehaviours.DoAddButton(_entitiesList);
            SerializedProperty entitiesProp = serializedObject.FindProperty(nameof(AudioAsset.Entities));
            SerializedProperty newEntity = entitiesProp.GetArrayElementAtIndex(_entitiesList.count - 1);
            ResetEntitySerializedProperties(newEntity);

            AssignID(newEntity, BroAudioType.None);

            return newEntity;
        }

        public void SetClipList(SerializedProperty clipListProp, int index, AudioClip clip)
        {
            clipListProp.InsertArrayElementAtIndex(index);
            SerializedProperty elementProp = clipListProp.GetArrayElementAtIndex(index);
            elementProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip).objectReferenceValue = clip;
            elementProp.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = AudioConstant.FullVolume;
        }

        public void SelectEntity(int id, out float entityVerticalPos)
        {
            entityVerticalPos = 0f;
            for (int i = 0; i < _entitiesList.count;i++)
            {
                var elementProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(i);
                int entityID = elementProp.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
                bool isTarget = entityID == id;
                elementProp.isExpanded = isTarget;
                if (isTarget)
                {
                    _entitiesList.index = i;
                    entityVerticalPos = i * _entitiesList.elementHeight;
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
            else if (IsTempReservedName(Asset.AssetName))
            {
                CurrInstruction = Instruction.AssetNaming_StartWithTemp;
                return false;
            }
            return true;
        }
    }
}