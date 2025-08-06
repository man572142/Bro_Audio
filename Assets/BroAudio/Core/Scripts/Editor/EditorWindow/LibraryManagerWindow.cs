using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using System.IO;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.Setting.PreferencesEditorWindow;

namespace Ami.BroAudio.Editor
{
    public partial class LibraryManagerWindow : EditorWindow, IHasCustomMenu
    {
        private const int AssetNameFontSize = 16;
        private const float BackButtonSize = 28f;
        private const float EntitiesFactoryRatio = 0.65f;

        public static event Action OnCloseLibraryManagerWindow;
        public static event Action OnLostFocusEvent;
        public static event Action OnSelectAsset;

        private readonly GapDrawingHelper _verticalGapDrawer = new GapDrawingHelper();
        private readonly BroInstructionHelper _instruction = new BroInstructionHelper();
        //private readonly EditorFlashingHelper _flasingHelper = new EditorFlashingHelper(Color.white, 1f, Ease.InCubic);
        private readonly IUniqueIDGenerator _idGenerator = new IdGenerator();

        private List<string> _allAssetGUIDs;
        private ReorderableList _assetReorderableList;
        private int _currSelectedAssetIndex = -1;
        private Dictionary<string, AudioAssetEditor> _assetEditorDict = new Dictionary<string, AudioAssetEditor>();
        private bool _hasAssetListReordered;
        private bool _isInEntitiesEditMode;
        private bool _hasOutputAssetPath;
        private bool _showSettings;

        private Vector2 _assetListScrollPos = Vector2.zero;
        private Vector2 _entitiesScrollPos = Vector2.zero;

        private static Vector2 EntitiesHeaderSize => new Vector2(200f, EditorGUIUtility.singleLineHeight * 2);
        private static float DefaultLayoutPadding => GUI.skin.box.padding.top;
        public IUniqueIDGenerator IDGenerator => _idGenerator;
        private EditorSetting EditorSetting => BroEditorUtility.EditorSetting;

        [MenuItem(LibraryManagerMenuPath, false, LibraryManagerMenuIndex)]
        public static LibraryManagerWindow ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(LibraryManagerWindow));
            Texture titleIcon = Resources.Load<Texture>(MainLogoPath);
            window.titleContent = new GUIContent(BroName.MenuItem_LibraryManager, titleIcon);
            window.Show();
            return window as LibraryManagerWindow;
        }

        public static void ShowWindowAndLocateToEntity(string guid, int id)
        {
            var window = ShowWindow();
            window.SelectAsset(guid);
            window.SelectEntity(guid, id);
        }

        public void SelectAsset(string guid)
        {
            int index = _allAssetGUIDs.IndexOf(guid);
            if (index >= 0)
            {
                _assetReorderableList.index = index;
                OnSelect(_assetReorderableList);
            }
        }

        public void RemoveAssetEditor(string guid)
        {
            if (_assetEditorDict.TryGetValue(guid, out var editor))
            {
                DestroyImmediate(editor);
            }
            _assetEditorDict.Remove(guid);
            _allAssetGUIDs.Remove(guid);
        }

        private void OnFocus()
        {
            EditorAudioPreviewer.Instance.OnPlaybackIndicatorUpdate += Repaint;
        }

        private void OnLostFocus()
        {
            EditorAudioPreviewer.Instance.StopAllClips();
            EditorAudioPreviewer.Instance.OnPlaybackIndicatorUpdate -= Repaint;
            OnLostFocusEvent?.Invoke();
        }

        private void OnEnable()
        {
            if (TryGetCoreData(out var coreData))
            {
                _allAssetGUIDs = coreData.GetGUIDList();
                _hasOutputAssetPath = Directory.Exists(EditorSetting.AssetOutputPath);

                InitEditorDictionary();
                InitReorderableList();
                RefreshAssetEditors(_assetReorderableList);
            }

            Undo.undoRedoPerformed += Repaint;
            AudioEntityPropertyDrawer.OnExpandAll += ResetEntitiesScrollPos;

            if (EditorSetting.OpenLastEditAudioAsset && !string.IsNullOrEmpty(EditorSetting.LastEditAudioAsset))
            {
                SelectAsset(EditorSetting.LastEditAudioAsset);
                _isInEntitiesEditMode = true;
            }

            InitBackgroundLogo();
        }

        private void OnDisable()
        {
            OnCloseLibraryManagerWindow?.Invoke();
            foreach (AudioAssetEditor editor in _assetEditorDict.Values)
            {
                DestroyImmediate(editor);
            }
            Undo.undoRedoPerformed -= Repaint;
            AudioEntityPropertyDrawer.OnExpandAll -= ResetEntitiesScrollPos;

            if (_hasAssetListReordered)
            {
                ReorderAssets(_allAssetGUIDs);
            }

            OnCloseLibraryManagerWindow = null;
            OnSelectAsset = null;
            OnLostFocusEvent = null;
        }

        #region Initialization
        private void InitEditorDictionary()
        {
            _assetEditorDict.Clear();
            foreach (string guid in _allAssetGUIDs)
            {
                if (!string.IsNullOrEmpty(guid) && !_assetEditorDict.ContainsKey(guid))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

                    AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(asset, typeof(AudioAssetEditor)) as AudioAssetEditor;
                    editor.Init(_idGenerator);
                    _assetEditorDict.Add(guid, editor);
                }
            }
        }

        private void InitReorderableList()
        {
            _assetReorderableList = new ReorderableList(_allAssetGUIDs, typeof(string))
            {
                drawHeaderCallback = OnDrawHeader, 
                onAddCallback = OnAdd, 
                onRemoveCallback = OnRemove,
                drawElementCallback = OnDrawElement,
                onSelectCallback = OnSelect,
                onReorderCallback = OnReordered
            };

            void OnDrawHeader(Rect rect)
            {
                EditorGUI.LabelField(rect, "Asset List");
            }

            void OnAdd(ReorderableList list)
            {
                ShowCreateAssetAskName();
                GUIUtility.ExitGUI();
            }

            void OnRemove(ReorderableList list)
            {
                string path = AssetDatabase.GUIDToAssetPath(_allAssetGUIDs[list.index]);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.Refresh();
                // AssetPostprocessorEditor will do the rest
            }

            void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (_assetEditorDict.TryGetValue(_allAssetGUIDs[index], out var editor))
                {
                    if (editor.Asset == null)
                        return;

                    EditorGUI.LabelField(rect, editor.Asset.AssetName, GUIStyleHelper.RichText);
                }

                if (index == _currSelectedAssetIndex && Event.current.isMouse && Event.current.clickCount >= 2)
                {
                    _isInEntitiesEditMode = true;
                }

                HandleContextMenu(rect, editor.Asset as AudioAsset);
            }

            void OnReordered(ReorderableList list)
            {
                _hasAssetListReordered = true;
            }
        }

        private void HandleContextMenu(Rect rect, AudioAsset editorAsset)
        {
            var evt = Event.current;
            if (evt.type == EventType.ContextClick && editorAsset && rect.Contains(evt.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Ping"), false,() => EditorGUIUtility.PingObject(editorAsset));
                menu.ShowAsContext();
            }
        }

        private void OnSelect(ReorderableList list)
        {
            if (list.index != _currSelectedAssetIndex)
            {
                OnSelectAsset?.Invoke();
                _currSelectedAssetIndex = list.index;
                EditorAudioPreviewer.Instance.StopAllClips();
                RefreshAssetEditors(list);
                if(EditorSetting.OpenLastEditAudioAsset)
                {
                    EditorSetting.LastEditAudioAsset = _allAssetGUIDs[_currSelectedAssetIndex];
                    EditorUtility.SetDirty(EditorSetting);
                }
            }
        }

        private void RefreshAssetEditors(ReorderableList list)
        {
            foreach (var pair in _assetEditorDict)
            {
                string guid = pair.Key;
                var editor = pair.Value;
                if (list.index >= 0 && guid == _allAssetGUIDs[list.index])
                {
                    editor.RemoveEntitiesListener();
                    editor.AddEntitiesListener();
                    editor.Verify();
                }
                else
                {
                    editor.RemoveEntitiesListener();
                }
            }
        }
        #endregion

        private bool TryGetCurrentAssetEditor(out AudioAssetEditor editor)
        {
            editor = null;
            if (_allAssetGUIDs == null || _assetReorderableList == null)
            {
                return false;
            }

            if (_allAssetGUIDs.Count > 0 && _assetReorderableList.index >= 0)
            {
                int index = Mathf.Clamp(_assetReorderableList.index, 0, _allAssetGUIDs.Count - 1);
                if (_assetEditorDict.TryGetValue(_allAssetGUIDs[index], out editor))
                {
                    return true;
                }
            }
            else if (_assetEditorDict.TryGetValue(BroName.TempAssetName, out editor))
            {
                return true;
            }
            return false;
        }

        #region Asset Creation
        private void ShowCreateAssetAskName()
        {
            // In the following case. List has better performance than IEnumerable, even with a ToList() method.
            List<string> assetNames = _assetEditorDict.Values.Select(x => x.Asset.AssetName).ToList();
            AssetNameEditorWindow.ShowWindow(assetNames, assetName => CreateAsset(assetName));
        }

        private AudioAssetEditor CreateAsset(string entityName)
        {
            if (!TryGetNewPath(entityName, out string path, out string fileName))
            {
                return null;
            }

            var newAsset = ScriptableObject.CreateInstance(typeof(AudioAsset));
            AssetDatabase.CreateAsset(newAsset, path);
            AddNewAssetToCoreData(newAsset);
            AssetDatabase.SaveAssets();

            AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(newAsset, typeof(AudioAssetEditor)) as AudioAssetEditor;
            string guid = AssetDatabase.AssetPathToGUID(path);
            editor.Init(_idGenerator);
            editor.SetData(guid, fileName);

            _assetEditorDict.Add(guid, editor);
            _allAssetGUIDs.Add(guid);

            _assetReorderableList.index = _assetReorderableList.count - 1;
            return editor;
        }

        private bool TryGetNewPath(string entityName, out string path, out string result)
        {
            path = string.Empty;
            result = entityName;
            if (!string.IsNullOrEmpty(AssetOutputPath))
            {
                int index = 0;
                path = GetNewAssetPath(entityName);
                while (File.Exists(path))
                {
                    index++;
                    result = entityName + index.ToString();
                    path = GetNewAssetPath(result);
                }
                return true;
            }
            return false;

            string GetNewAssetPath(string fileName)
            {
                return GetFilePath(AssetOutputPath, fileName + ".asset");
            }
        }
        #endregion

        #region GUI Drawing
        private void OnGUI()
        {
            _verticalGapDrawer.DrawLineCount = 0;

            if (!_hasOutputAssetPath)
            {
                DrawAssetOutputPath();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(_verticalGapDrawer.GetSpace());

                if (_isInEntitiesEditMode && TryGetCurrentAssetEditor(out var editor))
                {
                    DrawEntitiesList(editor);
                }
                else
                {
                    SplitRectHorizontal(position, EntitiesFactoryRatio, _verticalGapDrawer.SingleLineSpace, out Rect entitiesFactoryRect, out Rect assetListRect);

                    DrawEntityFactory(entitiesFactoryRect);
                    GUILayout.Space(_verticalGapDrawer.GetSpace());

                    DrawAssetList(assetListRect);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetOutputPath()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(AssetOutputPathLabel.ToBold(), GUIStyleHelper.MiddleCenterRichText);
            Vector2 halfLineSize = new Vector2(position.width * 0.5f, EditorGUIUtility.singleLineHeight);
            Rect helpBoxRect = GUILayoutUtility.GetRect(halfLineSize.x, EditorGUIUtility.singleLineHeight * 2).GetHorizontalCenterRect(halfLineSize.x, EditorGUIUtility.singleLineHeight * 2);
            RichTextHelpBox(helpBoxRect, AssetOutputPathMissing, MessageType.Error);
            Rect assetOutputRect = GUILayoutUtility.GetRect(halfLineSize.x, halfLineSize.y).GetHorizontalCenterRect(halfLineSize.x, halfLineSize.y);
            BroEditorUtility.DrawAssetOutputPath(assetOutputRect, _instruction, () => _hasOutputAssetPath = true);
        }

        private void DrawAssetList(Rect assetListRect)
        {
            assetListRect.width -= _verticalGapDrawer.GetTotalSpace() - _verticalGapDrawer.SingleLineSpace;
            assetListRect.height -= DefaultLayoutPadding * 2;
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(assetListRect.width), GUILayout.Height(assetListRect.height));
            {
                _assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
                {
                    _assetReorderableList.DoLayoutList();
                }
                EditorGUILayout.EndScrollView();

                if (TryGetCurrentAssetEditor(out var editor))
                {
                    DrawIssueMessage(editor);
                }
                else if (_assetReorderableList.count > 0)
                {
                    EditorGUILayout.HelpBox(_instruction.GetText(Instruction.LibraryManager_ModifyAsset), MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();

            void DrawIssueMessage(AudioAssetEditor assetEditor)
            {
                string assetName = assetEditor.Asset.AssetName.ToBold().SetColor(Color.white);
                string text = _instruction.GetText(assetEditor.CurrInstruction);

                switch (assetEditor.CurrInstruction)
                {
                    case Instruction.AssetNaming_StartWithTemp:
                        RichTextHelpBox(String.Format(text, assetName), MessageType.Error);
                        break;
                    case Instruction.None:
                        EditorGUILayout.HelpBox(_instruction.GetText(Instruction.LibraryManager_ModifyAsset), MessageType.Info);
                        break;
                    default:
                        EditorGUILayout.HelpBox(text, MessageType.Error);
                        break;
                }
            }
        }

        private void DrawEntitiesList(AudioAssetEditor editor)
        {
            Rect rect = new Rect(position);
            rect.width -= _verticalGapDrawer.GetTotalSpace();
            rect.height -= DefaultLayoutPadding * 2;
            float offsetX = _verticalGapDrawer.GetTotalSpace() + DefaultLayoutPadding;
            float offsetY = ReorderableList.Defaults.padding + DefaultLayoutPadding - 1f;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(rect.width), GUILayout.Height(rect.height));
            {
                _entitiesScrollPos = EditorGUILayout.BeginScrollView(_entitiesScrollPos);
                {
                    DrawEntitiesHeader(editor.serializedObject, editor.SetAssetName);
                    editor.DrawEntitiesList(out float listHeight);
                    float compensateHeight = GetScrollPosCompensateHeight(listHeight);
                    if (compensateHeight > 0f)
                    {
                        GUILayout.Space(compensateHeight);
                    }
                }
                EditorGUILayout.EndScrollView();
                EditorAudioPreviewer.Instance.PlaybackIndicator?.Draw(rect.Scoping(position, new Vector2(offsetX, offsetY)), -_entitiesScrollPos);
            }
            EditorGUILayout.EndVertical();
        }

        private float GetScrollPosCompensateHeight(float listHeight)
        {
            float headerHeight = EntitiesHeaderSize.y + (DefaultLayoutPadding * 2) + ReorderableList.Defaults.padding;
            float scrollViewHeight = listHeight - (position.height - headerHeight);
            return _entitiesScrollPos.y - scrollViewHeight;
        }

        private void ResetEntitiesScrollPos(bool isExpanded)
        {
            if (!isExpanded)
            {
                _entitiesScrollPos = new Vector2(_entitiesScrollPos.x, 0f);
            }
        }

        // The ReorderableList default header background GUIStyle has set fixedHeight to non-0 and stretchHeight to false, which is unreasonable...
        // Use another style or Draw it manually could solve the problem and accept more customization.
        private void DrawEntitiesHeader(SerializedObject serializedAsset, Action<string> onAssetNameChanged)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent(IconConstant.BackButton), GUILayout.Width(BackButtonSize), GUILayout.Height(BackButtonSize)))
                {
                    _isInEntitiesEditMode = false;
                    _assetReorderableList.index = -1;
                }
                GUILayout.Space(10f);

                Rect headerRect = GUILayoutUtility.GetRect(EntitiesHeaderSize.x, EntitiesHeaderSize.y);
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.skin.window.Draw(headerRect, false, false, false, false);
                    EditorStyles.textField.Draw(headerRect.PolarCoordinates(-1f).AdjustWidth(1f), headerRect.Contains(Event.current.mousePosition), false, false, false);
                    EditorGUI.DrawRect(headerRect.PolarCoordinates(-2f).AdjustWidth(1f), new Color(1f, 1f, 1f, 0.1f));
                }

                var nameProp = serializedAsset.FindBackingFieldProperty(nameof(AudioAsset.AssetName));
                DrawAssetNameField(headerRect, nameProp, onAssetNameChanged);

                GUILayout.FlexibleSpace();

                _showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showSettings, "Settings");
                if (_showSettings)
                {
                    var groupProp = serializedAsset.FindProperty(nameof(AudioAsset.Group));
                    EditorGUI.BeginChangeCheck();
                    groupProp.objectReferenceValue = (PlaybackGroup)EditorGUILayout.ObjectField(groupProp.objectReferenceValue, typeof(PlaybackGroup), false);
                    if(EditorGUI.EndChangeCheck())
                    {
                        serializedAsset.ApplyModifiedProperties();
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetNameField(Rect headerRect, SerializedProperty nameProp, Action<string> onAssetNameChanged)
        {
            GUIStyle wordWrapStyle = new GUIStyle(GUIStyleHelper.MiddleCenterRichText);
            wordWrapStyle.wordWrap = true;
            wordWrapStyle.fontSize = AssetNameFontSize;

            string displayName = GetDisplayName();
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.DelayedTextField(headerRect, displayName, wordWrapStyle);

            if (EditorGUI.EndChangeCheck()
                && !newName.Equals(nameProp.stringValue) && !newName.Equals(displayName) && IsValidAssetName(newName))
            {
                nameProp.stringValue = newName;
                onAssetNameChanged?.Invoke(newName);
            }

            string GetDisplayName()
            {
                if (string.IsNullOrEmpty(nameProp.stringValue) || IsTempReservedName(nameProp.stringValue))
                {
                    return _instruction.GetText(Instruction.LibraryManager_NameTempAssetHint);
                }
                return nameProp.stringValue;
            }
        }

        private bool IsValidAssetName(string newName)
        {
            if (IsInvalidName(newName, out ValidationErrorCode code))
            {
                switch (code)
                {
                    case ValidationErrorCode.StartWithNumber:
                        ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_StartWithNumber)), 2f);
                        break;
                    case ValidationErrorCode.ContainsInvalidWord:
                        ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_ContainsInvalidWords)), 2f);
                        break;
                    case ValidationErrorCode.ContainsWhiteSpace:
                        ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_ContainsWhiteSpace)), 2f);
                        break;
                    case ValidationErrorCode.IsDuplicate:
                        ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_IsDuplicated)), 2f);
                        break;
                }
                return false;
            }
            else if (IsTempReservedName(newName))
            {
                string text = string.Format(_instruction.GetText(Instruction.AssetNaming_StartWithTemp), newName);
                ShowNotification(new GUIContent(text), 2f);
                return false;
            }
            return true;
        }
        #endregion

        private void SelectEntity(string guid, int id)
        {
            _isInEntitiesEditMode = true;
            if (_assetEditorDict.TryGetValue(guid, out var editor))
            {
                editor.SelectEntity(id, out float entityPos);
                entityPos += EntitiesHeaderSize.y + ReorderableList.Defaults.padding + DefaultLayoutPadding;
                _entitiesScrollPos = new Vector2(_entitiesScrollPos.x, entityPos);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddSeparator(string.Empty);
            menu.AddDisabledItem(new GUIContent("Bro Audio"));
            menu.AddItem(new GUIContent("Default Window Size"), false, () => position = new Rect(position.position, DefaultWindowSize));
            menu.AddItem(new GUIContent(ShowPlayButtonWhenCollapsed), EditorSetting.ShowPlayButtonWhenEntityCollapsed, () => 
            {
                EditorSetting.ShowPlayButtonWhenEntityCollapsed = !EditorSetting.ShowPlayButtonWhenEntityCollapsed;
                EditorUtility.SetDirty(EditorSetting);
            });
            menu.AddItem(new GUIContent(OpenLastEditedAssetLabel), EditorSetting.OpenLastEditAudioAsset, () => 
            {
                EditorSetting.OpenLastEditAudioAsset = !EditorSetting.OpenLastEditAudioAsset;
                EditorSetting.LastEditAudioAsset = string.Empty;
                if(EditorSetting.OpenLastEditAudioAsset && _assetReorderableList.count > 0 &&  _assetReorderableList.index >= 0)
                {
                    EditorSetting.LastEditAudioAsset = _allAssetGUIDs[_assetReorderableList.index];
                    _isInEntitiesEditMode = true;
                }
                EditorUtility.SetDirty(EditorSetting);
            });
        }
    }
}