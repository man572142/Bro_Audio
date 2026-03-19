#if PACKAGE_LOCALIZATION
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
    public partial class ReorderableClips
    {
        private const float LocaleLabelWidth = 130f;
        private const string LocalizationPreviewPathPrefix = "localization_preview_";

        private SerializedProperty _localizationTableProp;
        private SerializedProperty _localizationEntryProp;

        private List<int> _localizationListData;
        private ReorderableList _localizationList;

        private bool HasLocalizationTableClip
        {
            get
            {
                string tableName = _localizationTableProp?.FindPropertyRelative("m_TableCollectionName")?.stringValue;
                string entryKey = _localizationEntryProp?.FindPropertyRelative("m_Key")?.stringValue;
                return !string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(entryKey);
            }
        }

        private void InitLocalization(UnityEditor.SerializedObject serializedObject)
        {
            _localizationTableProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationTable);
            _localizationEntryProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationEntry);

            _localizationListData = new List<int>();
            _localizationList = new ReorderableList(_localizationListData, typeof(int),
                draggable: false, displayHeader: true, displayAddButton: false, displayRemoveButton: false);
            _localizationList.drawHeaderCallback = DrawLocalizationHeader;
            _localizationList.drawElementCallback = OnDrawLocalizationListElement;
            _localizationList.elementHeightCallback = GetLocalizationElementHeight;

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void DisposeLocalization()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale locale)
        {
            SyncClipsWithLocales();
            _entity.Update();
        }

        private void UpdateLocalizationListCount()
        {
            int targetCount = 1 + (LocalizationSettings.AvailableLocales?.Locales?.Count ?? 0);
            while (_localizationListData.Count < targetCount)
                _localizationListData.Add(0);
            while (_localizationListData.Count > targetCount)
                _localizationListData.RemoveAt(_localizationListData.Count - 1);
            SyncClipsWithLocales();
        }

        private bool IsClipsSyncedWithLocales()
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null)
                return true;

            var clipsProp = _reorderableList.serializedProperty;
            if (clipsProp.arraySize != availableLocales.Count)
                return false;

            for (int i = 0; i < availableLocales.Count; i++)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(i);
                string code = clipProp.FindPropertyRelative("Locale")?.FindPropertyRelative("m_Code")?.stringValue;
                if (code != availableLocales[i].Identifier.Code)
                    return false;
            }

            return true;
        }

        private void SyncClipsWithLocales()
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null)
                return;

            if (IsClipsSyncedWithLocales())
                return;

            var clipsProp = _reorderableList.serializedProperty;

            // Preserve existing Volume values by locale code before resizing.
            var savedVolumes = new Dictionary<string, float>();
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var existing = clipsProp.GetArrayElementAtIndex(i);
                string code = existing.FindPropertyRelative("Locale")?.FindPropertyRelative("m_Code")?.stringValue ?? string.Empty;
                var volProp = existing.FindPropertyRelative(nameof(BroAudioClip.Volume));
                if (!string.IsNullOrEmpty(code) && volProp != null)
                    savedVolumes[code] = volProp.floatValue;
            }

            clipsProp.arraySize = availableLocales.Count;

            for (int i = 0; i < availableLocales.Count; i++)
            {
                string code = availableLocales[i].Identifier.Code;
                var clipProp = clipsProp.GetArrayElementAtIndex(i);

                var codeProp = clipProp.FindPropertyRelative("Locale")?.FindPropertyRelative("m_Code");
                if (codeProp != null)
                    codeProp.stringValue = code;

                var volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
                if (volProp != null)
                    volProp.floatValue = savedVolumes.TryGetValue(code, out float savedVol) ? savedVol : AudioConstant.FullVolume;
            }

            _entity.ApplyModifiedProperties();
        }

        private void DrawLocalizationHeader(Rect rect)
        {
            HandleClipsDragAndDrop(rect);

            Rect labelRect = new Rect(rect) { width = HeaderLabelWidth };
            Rect remainRect = new Rect(rect) { width = rect.width - HeaderLabelWidth, x = labelRect.xMax };
            EditorScriptingExtension.SplitRectHorizontal(remainRect, 0.5f, 10f, out var multiclipOptionRect, out var masterVolRect);

            EditorGUI.LabelField(labelRect, "Clips");

            var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
            playMode = (MulticlipsPlayMode)EditorGUI.EnumPopup(multiclipOptionRect, playMode);
            _playModeProp.enumValueIndex = (int)playMode;

            DrawMasterVolume(masterVolRect);

            EditorGUI.LabelField(multiclipOptionRect.DissolveHorizontal(0.5f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.MiddleCenterRichText);
        }

        private void OnDrawLocalizationListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index == 0)
                DrawLocalizationTableRow(rect);
            else
                DrawLocalizationTableClipElement(rect, index - 1);
        }

        private void DrawLocalizationTableRow(Rect rect)
        {
            float halfWidth = (rect.width - Gap) * 0.5f;
            Rect tableRect = new Rect(rect) { width = halfWidth };
            Rect entryRect = new Rect(rect) { x = tableRect.xMax + Gap, width = halfWidth };

            if (_localizationTableProp != null)
                DrawAssetTableDropdown(tableRect);
            if (_localizationEntryProp != null)
                DrawTableEntryDropdown(entryRect);
        }

        private const float VolumeFieldWidth = 100f;

        private void DrawLocalizationTableClipElement(Rect rect, int localeIndex)
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null || localeIndex >= availableLocales.Count)
                return;

            var locale = availableLocales[localeIndex];
            string localeCode = locale.Identifier.Code;
            AudioClip currentClip = TryGetClipFromTable(localeCode);
            string previewPath = LocalizationPreviewPathPrefix + localeCode;

            Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (GetLocalizationElementHeight(localeIndex + 1) - PlayButtonSize.y) * 0.5f;

            Rect localeRect = new Rect(rect) { x = buttonRect.xMax + Gap, width = LocaleLabelWidth };

            float usedWidth = buttonRect.width + Gap + LocaleLabelWidth + Gap;
            float clipWidth = rect.width - usedWidth - Gap - VolumeFieldWidth;
            Rect clipRect = new Rect(rect) { x = localeRect.xMax + Gap, width = clipWidth };
            Rect volumeRect = new Rect(rect) { x = clipRect.xMax + Gap, width = VolumeFieldWidth };

            EditorGUI.LabelField(localeRect, string.IsNullOrEmpty(localeCode) ? "(no locale)" : localeCode);

            if (currentClip != null)
            {
                bool isPlaying = string.Equals(_currentPlayingClipPath, previewPath);
                var image = GetPlaybackButtonIcon(isPlaying).image;
                var buttonContent = new GUIContent(image, EditorAudioPreviewer.IgnoreSettingTooltip);
                if (GUI.Button(buttonRect, buttonContent))
                {
                    if (isPlaying)
                        EditorAudioPreviewer.Instance.StopAllClips();
                    else
                        PreviewLocalizationClip(currentClip, previewPath);
                }
            }

            EditorGUI.BeginChangeCheck();
            var newClip = EditorGUI.ObjectField(clipRect, currentClip, typeof(AudioClip), false) as AudioClip;
            if (EditorGUI.EndChangeCheck())
            {
                TrySetClipInTable(localeCode, newClip);
            }

            var clipsProp = _reorderableList.serializedProperty;
            if (localeIndex < clipsProp.arraySize)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(localeIndex);
                var volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
                if (volumeProp != null)
                {
                    EditorGUI.BeginChangeCheck();
                    float newVol = EditorGUI.Slider(volumeRect, volumeProp.floatValue, 0f, AudioConstant.FullVolume);
                    if (EditorGUI.EndChangeCheck())
                    {
                        volumeProp.floatValue = newVol;
                        _entity.ApplyModifiedProperties();
                    }
                }
            }
        }

        private float GetLocalizationElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 4f;
        }

        private SerializedProperty GetLocalizationCurrentSelectedClip()
        {
            int locIndex = _localizationList?.index ?? -1;
            int clipIndex = locIndex - 1; // row 0 is table/entry dropdowns
            if (clipIndex >= 0 && clipIndex < _reorderableList.serializedProperty.arraySize)
            {
                if (_currSelectedClipIndex != clipIndex || _currSelectedClip == null)
                {
                    _currSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(clipIndex);
                    _currSelectedClipIndex = clipIndex;
                }
                return _currSelectedClip;
            }
            _currSelectedClip = null;
            return null;
        }

        private bool TryGetLocalizationSelectedClip(out AudioClip audioClip)
        {
            audioClip = null;
            int locIndex = _localizationList?.index ?? -1;
            int clipIndex = locIndex - 1;
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (clipIndex >= 0 && availableLocales != null && clipIndex < availableLocales.Count)
            {
                string localeCode = availableLocales[clipIndex].Identifier.Code;
                audioClip = TryGetClipFromTable(localeCode);
            }
            return audioClip != null;
        }

        private void PreviewLocalizationClip(AudioClip audioClip, string previewPath)
        {
            var currentEvent = Event.current;
            var req = currentEvent.CreatePreviewRequest(audioClip);
            GetBaseAndRandomValue(RandomFlag.Volume, _entity, out req.BaseMasterVolume, out req.MasterVolume);
            GetBaseAndRandomValue(RandomFlag.Pitch, _entity, out req.BasePitch, out req.Pitch);

            _onRequestClipPreview?.Invoke(previewPath, req);
            _currentPlayingClipPath = previewPath;
            EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(PreviewRect, req);
        }

        private void DrawAssetTableDropdown(Rect rect)
        {
            var tableNameProp = _localizationTableProp.FindPropertyRelative("m_TableCollectionName");
            if (tableNameProp == null)
                return;

            var collections = LocalizationEditorSettings.GetAssetTableCollections();
            string currentName = tableNameProp.stringValue;

            int selectedIndex = 0;
            var labels = new GUIContent[collections.Count + 1];
            labels[0] = new GUIContent("None");
            for (int i = 0; i < collections.Count; i++)
            {
                string name = collections[i].TableCollectionName;
                labels[i + 1] = new GUIContent(name);
                if (name == currentName)
                    selectedIndex = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(rect, selectedIndex, labels);
            if (EditorGUI.EndChangeCheck())
            {
                tableNameProp.stringValue = newIndex == 0 ? string.Empty : collections[newIndex - 1].TableCollectionName;
                var entryKeyProp = _localizationEntryProp?.FindPropertyRelative("m_Key");
                if (entryKeyProp != null)
                    entryKeyProp.stringValue = string.Empty;
                var entryKeyIdProp = _localizationEntryProp?.FindPropertyRelative("m_KeyId");
                if (entryKeyIdProp != null)
                    entryKeyIdProp.longValue = 0;
                _localizationTableProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawTableEntryDropdown(Rect rect)
        {
            var entryKeyProp = _localizationEntryProp.FindPropertyRelative("m_Key");
            if (entryKeyProp == null)
                return;

            var tableNameProp = _localizationTableProp?.FindPropertyRelative("m_TableCollectionName");
            string tableName = tableNameProp?.stringValue;
            if (string.IsNullOrEmpty(tableName))
            {
                EditorGUI.Popup(rect, 0, new GUIContent[] { new GUIContent("None") });
                return;
            }

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null || tableCollection.SharedData == null)
            {
                EditorGUI.Popup(rect, 0, new GUIContent[] { new GUIContent("None") });
                return;
            }

            var entries = tableCollection.SharedData.Entries;
            string currentKey = entryKeyProp.stringValue;

            int selectedIndex = 0;
            var labels = new GUIContent[entries.Count + 1];
            labels[0] = new GUIContent("None");
            for (int i = 0; i < entries.Count; i++)
            {
                labels[i + 1] = new GUIContent(entries[i].Key);
                if (entries[i].Key == currentKey)
                    selectedIndex = i + 1;
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(rect, selectedIndex, labels);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    entryKeyProp.stringValue = string.Empty;
                    var entryKeyIdProp = _localizationEntryProp.FindPropertyRelative("m_KeyId");
                    if (entryKeyIdProp != null)
                        entryKeyIdProp.longValue = 0;
                }
                else
                {
                    var entry = entries[newIndex - 1];
                    entryKeyProp.stringValue = entry.Key;
                    var entryKeyIdProp = _localizationEntryProp.FindPropertyRelative("m_KeyId");
                    if (entryKeyIdProp != null)
                        entryKeyIdProp.longValue = entry.Id;
                }
                _localizationEntryProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private AudioClip TryGetClipFromTable(string localeCode)
        {
            if (_localizationTableProp == null || _localizationEntryProp == null
                || string.IsNullOrEmpty(localeCode))
                return null;

            string tableName = _localizationTableProp.FindPropertyRelative("m_TableCollectionName")?.stringValue;
            if (string.IsNullOrEmpty(tableName))
                return null;

            string entryKey = _localizationEntryProp.FindPropertyRelative("m_Key")?.stringValue;
            if (string.IsNullOrEmpty(entryKey))
                return null;

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null)
                return null;

            var locale = LocalizationSettings.AvailableLocales?.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
                return null;

            var table = tableCollection.GetTable(locale.Identifier) as AssetTable;
            var entry = table?.GetEntry(entryKey);
            if (entry == null || string.IsNullOrEmpty(entry.Guid))
                return null;

            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(entry.Guid));
        }

        private void TrySetClipInTable(string localeCode, AudioClip clip)
        {
            if (_localizationTableProp == null || _localizationEntryProp == null
                || string.IsNullOrEmpty(localeCode))
                return;

            string tableName = _localizationTableProp.FindPropertyRelative("m_TableCollectionName")?.stringValue;
            if (string.IsNullOrEmpty(tableName))
                return;

            string entryKey = _localizationEntryProp.FindPropertyRelative("m_Key")?.stringValue;
            if (string.IsNullOrEmpty(entryKey))
                return;

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null)
                return;

            var locale = LocalizationSettings.AvailableLocales?.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
                return;

            var table = tableCollection.GetTable(locale.Identifier) as AssetTable;
            if (table == null)
                return;

            string guid = clip != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip)) : string.Empty;
            var entry = table.GetEntry(entryKey);
            if (entry == null)
                tableCollection.AddAssetToTable(table, entryKey, clip);
            else
            {
                entry.Guid = guid;
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
