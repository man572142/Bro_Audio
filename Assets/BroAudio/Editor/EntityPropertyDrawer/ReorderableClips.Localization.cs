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
        private const string LocalizationPreviewPathPrefix = "localization_preview_";

        // Unity Localization serialized property field names
        private const string TableCollectionNameField = "m_TableCollectionName";
        private const string EntryKeyField = "m_Key";
        private const string EntryKeyIdField = "m_KeyId";
        private const string LocaleCodeField = "m_Code";

        private SerializedProperty _localizationTableProp;
        private SerializedProperty _localizationEntryProp;

        private List<int> _localizationListData;
        private ReorderableList _localizationList;

        // Cached dropdown labels to avoid per-frame allocations
        private GUIContent[] _cachedTableLabels;
        private string _cachedTableSelection;
        private GUIContent[] _cachedEntryLabels;
        private string _cachedEntryTableName;
        private string _cachedEntrySelection;

        private float LocaleLabelWidth => MulticlipsValueFieldWidth + 10f;
        private bool HasLocalizationTableClip
        {
            get
            {
                string tableName = _localizationTableProp?.FindPropertyRelative(TableCollectionNameField)?.stringValue;
                string entryKey = _localizationEntryProp?.FindPropertyRelative(EntryKeyField)?.stringValue;
                return !string.IsNullOrEmpty(tableName) && !string.IsNullOrEmpty(entryKey);
            }
        }

        private void InitLocalization(SerializedObject serializedObject)
        {
            _localizationTableProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationTable);
            _localizationEntryProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationEntry);

            _localizationListData = new List<int>();
            _localizationList = new ReorderableList(_localizationListData, typeof(int),
                draggable: false, displayHeader: true, displayAddButton: false, displayRemoveButton: false);
            _localizationList.drawHeaderCallback = OnDrawHeader;
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
            _cachedTableLabels = null;
            _cachedEntryLabels = null;
            SyncClipsWithLocales();
            _entity.Update();
        }

        private void UpdateLocalizationListCount()
        {
            int targetCount = 1 + (LocalizationSettings.AvailableLocales?.Locales?.Count ?? 0);
            while (_localizationListData.Count < targetCount)
            {
                _localizationListData.Add(0);
            }

            while (_localizationListData.Count > targetCount)
            {
                _localizationListData.RemoveAt(_localizationListData.Count - 1);
            }

            SyncClipsWithLocales();
        }

        private bool IsClipsSyncedWithLocales()
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null)
            {
                return true;
            }

            var clipsProp = _reorderableList.serializedProperty;
            if (clipsProp.arraySize != availableLocales.Count)
            {
                return false;
            }

            for (int i = 0; i < availableLocales.Count; i++)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(i);
                string code = clipProp.FindPropertyRelative(nameof(BroAudioClip.Locale))?.FindPropertyRelative(LocaleCodeField)?.stringValue;
                if (code != availableLocales[i].Identifier.Code)
                {
                    return false;
                }
            }

            return true;
        }

        private void SyncClipsWithLocales()
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null)
            {
                return;
            }

            if (IsClipsSyncedWithLocales())
            {
                return;
            }

            var clipsProp = _reorderableList.serializedProperty;

            // Preserve existing Volume values by locale code before resizing.
            var savedVolumes = new Dictionary<string, float>();
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var existing = clipsProp.GetArrayElementAtIndex(i);
                string code = existing.FindPropertyRelative(nameof(BroAudioClip.Locale))?.FindPropertyRelative(LocaleCodeField)?.stringValue ?? string.Empty;
                var volProp = existing.FindPropertyRelative(nameof(BroAudioClip.Volume));
                if (!string.IsNullOrEmpty(code) && volProp != null)
                {
                    savedVolumes[code] = volProp.floatValue;
                }
            }

            clipsProp.arraySize = availableLocales.Count;

            for (int i = 0; i < availableLocales.Count; i++)
            {
                string code = availableLocales[i].Identifier.Code;
                var clipProp = clipsProp.GetArrayElementAtIndex(i);

                var codeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Locale))?.FindPropertyRelative(LocaleCodeField);
                if (codeProp != null)
                {
                    codeProp.stringValue = code;
                }

                var volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
                if (volProp != null)
                {
                    volProp.floatValue = savedVolumes.TryGetValue(code, out float savedVol) ? savedVol : AudioConstant.FullVolume;
                }
            }

            _entity.ApplyModifiedProperties();
        }

        private void OnDrawLocalizationListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index == 0)
            {
                DrawLocalizationTableRow(rect);
            }
            else
            {
                DrawLocalizationTableClipElement(rect, index - 1);
            }
        }

        private void DrawLocalizationTableRow(Rect rect)
        {
            float halfWidth = (rect.width - Gap) * 0.5f;
            Rect tableRect = new Rect(rect) { width = halfWidth };
            Rect entryRect = new Rect(rect) { x = tableRect.xMax + Gap, width = halfWidth };

            if (_localizationTableProp != null)
            {
                DrawAssetTableDropdown(tableRect);
            }

            if (_localizationEntryProp != null)
            {
                DrawTableEntryDropdown(entryRect);
            }
        }

        private void DrawLocalizationTableClipElement(Rect rect, int localeIndex)
        {
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales == null || localeIndex >= availableLocales.Count)
            {
                return;
            }

            var locale = availableLocales[localeIndex];
            string localeCode = locale.Identifier.Code;
            AudioClip currentClip = TryGetClipFromTable(localeCode);
            string previewPath = LocalizationPreviewPathPrefix + localeCode;

            Rect localeRect = new Rect(rect) { width = LocaleLabelWidth, x = rect.xMax - LocaleLabelWidth };

            Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (GetLocalizationElementHeight(localeIndex + 1) - PlayButtonSize.y) * 0.5f;

            float remainWidth = rect.width - buttonRect.width - localeRect.width;
            Rect clipRect = new Rect(rect) { x = buttonRect.xMax + Gap, width = (remainWidth * ObjectPickerRatio) - Gap };

            Rect volIconRect = new Rect(rect) { x = clipRect.xMax + Gap, width = SliderLabelWidth };
            Rect volumeRect = new Rect(rect) { x = volIconRect.xMax, width = (remainWidth * (1 - ObjectPickerRatio)) - Gap - SliderLabelWidth };

            if (currentClip != null)
            {
                bool isPlaying = string.Equals(_currentPlayingClipPath, previewPath);
                var image = GetPlaybackButtonIcon(isPlaying).image;
                var buttonContent = new GUIContent(image, EditorAudioPreviewer.IgnoreSettingTooltip);
                if (GUI.Button(buttonRect, buttonContent))
                {
                    if (isPlaying)
                    {
                        EditorAudioPreviewer.Instance.StopAllClips();
                    }
                    else
                    {
                        PreviewLocalizationClip(currentClip, previewPath);
                    }
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
                    EditorGUI.LabelField(volIconRect, EditorGUIUtility.IconContent(IconConstant.AudioSpeakerOn));
                    EditorGUI.BeginChangeCheck();
                    float newVol = BroEditorUtility.DrawVolumeSlider(volumeRect, volumeProp.floatValue, out bool hasChanged, out _);
                    if (hasChanged)
                    {
                        volumeProp.floatValue = newVol;
                        _entity.ApplyModifiedProperties();
                    }
                }
            }

            EditorGUI.LabelField(localeRect, string.IsNullOrEmpty(localeCode) ? "(no locale)" : localeCode, GUIStyleHelper.MiddleCenterText);
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

        public bool TryGetLocalizationEntityPreviewClip(out AudioClip audioClip, out int clipIndex)
        {
            audioClip = null;
            clipIndex = 0;

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            var selectedLocale = LocalizationSettings.SelectedLocale
                ?? LocalizationSettings.ProjectLocale;
            if (selectedLocale == null && locales != null && locales.Count > 0)
                selectedLocale = locales[0];
            if (selectedLocale == null)
                return false;

            string localeCode = selectedLocale.Identifier.Code;
            audioClip = TryGetClipFromTable(localeCode);
            if (audioClip == null)
                return false;

            int found = FindClipIndexByLocaleCode(localeCode);
            if (found >= 0)
                clipIndex = found;

            return true;
        }

        private int FindClipIndexByLocaleCode(string localeCode)
        {
            var clipsProp = _reorderableList.serializedProperty;
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var clipProp = clipsProp.GetArrayElementAtIndex(i);
                string code = clipProp.FindPropertyRelative(nameof(BroAudioClip.Locale))?.FindPropertyRelative(LocaleCodeField)?.stringValue;
                if (code == localeCode)
                    return i;
            }
            return -1;
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

        private static readonly GUIContent[] NoneOnlyLabels = { new GUIContent("None") };

        private void DrawAssetTableDropdown(Rect rect)
        {
            var tableNameProp = _localizationTableProp.FindPropertyRelative(TableCollectionNameField);
            if (tableNameProp == null)
            {
                return;
            }

            var collections = LocalizationEditorSettings.GetAssetTableCollections();
            string currentName = tableNameProp.stringValue;

            if (_cachedTableLabels == null || _cachedTableSelection != currentName || _cachedTableLabels.Length != collections.Count + 1)
            {
                _cachedTableSelection = currentName;
                _cachedTableLabels = new GUIContent[collections.Count + 1];
                _cachedTableLabels[0] = new GUIContent("None");
                for (int i = 0; i < collections.Count; i++)
                {
                    _cachedTableLabels[i + 1] = new GUIContent(collections[i].TableCollectionName);
                }
            }

            int selectedIndex = 0;
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].TableCollectionName == currentName)
                {
                    selectedIndex = i + 1;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(rect, selectedIndex, _cachedTableLabels);
            if (EditorGUI.EndChangeCheck())
            {
                tableNameProp.stringValue = newIndex == 0 ? string.Empty : collections[newIndex - 1].TableCollectionName;
                var entryKeyProp = _localizationEntryProp?.FindPropertyRelative(EntryKeyField);
                if (entryKeyProp != null)
                {
                    entryKeyProp.stringValue = string.Empty;
                }

                var entryKeyIdProp = _localizationEntryProp?.FindPropertyRelative(EntryKeyIdField);
                if (entryKeyIdProp != null)
                {
                    entryKeyIdProp.longValue = 0;
                }

                _cachedTableLabels = null;
                _cachedEntryLabels = null;
                _localizationTableProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawTableEntryDropdown(Rect rect)
        {
            var entryKeyProp = _localizationEntryProp.FindPropertyRelative(EntryKeyField);
            if (entryKeyProp == null)
            {
                return;
            }

            var tableNameProp = _localizationTableProp?.FindPropertyRelative(TableCollectionNameField);
            string tableName = tableNameProp?.stringValue;
            if (string.IsNullOrEmpty(tableName))
            {
                EditorGUI.Popup(rect, 0, NoneOnlyLabels);
                return;
            }

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null || tableCollection.SharedData == null)
            {
                EditorGUI.Popup(rect, 0, NoneOnlyLabels);
                return;
            }

            var entries = tableCollection.SharedData.Entries;
            string currentKey = entryKeyProp.stringValue;

            if (_cachedEntryLabels == null || _cachedEntryTableName != tableName || _cachedEntrySelection != currentKey || _cachedEntryLabels.Length != entries.Count + 1)
            {
                _cachedEntryTableName = tableName;
                _cachedEntrySelection = currentKey;
                _cachedEntryLabels = new GUIContent[entries.Count + 1];
                _cachedEntryLabels[0] = new GUIContent("None");
                for (int i = 0; i < entries.Count; i++)
                {
                    _cachedEntryLabels[i + 1] = new GUIContent(entries[i].Key);
                }
            }

            int selectedIndex = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key == currentKey)
                {
                    selectedIndex = i + 1;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(rect, selectedIndex, _cachedEntryLabels);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                {
                    entryKeyProp.stringValue = string.Empty;
                    var entryKeyIdProp = _localizationEntryProp.FindPropertyRelative(EntryKeyIdField);
                    if (entryKeyIdProp != null)
                    {
                        entryKeyIdProp.longValue = 0;
                    }
                }
                else
                {
                    var entry = entries[newIndex - 1];
                    entryKeyProp.stringValue = entry.Key;
                    var entryKeyIdProp = _localizationEntryProp.FindPropertyRelative(EntryKeyIdField);
                    if (entryKeyIdProp != null)
                    {
                        entryKeyIdProp.longValue = entry.Id;
                    }
                }
                _cachedEntryLabels = null;
                _localizationEntryProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private bool TryGetAssetTableAndEntry(string localeCode, out AssetTableCollection tableCollection, out AssetTable table, out string entryKey)
        {
            tableCollection = null;
            table = null;
            entryKey = null;

            if (_localizationTableProp == null || _localizationEntryProp == null
                || string.IsNullOrEmpty(localeCode))
            {
                return false;
            }

            string tableName = _localizationTableProp.FindPropertyRelative(TableCollectionNameField)?.stringValue;
            if (string.IsNullOrEmpty(tableName))
            {
                return false;
            }

            entryKey = _localizationEntryProp.FindPropertyRelative(EntryKeyField)?.stringValue;
            if (string.IsNullOrEmpty(entryKey))
            {
                return false;
            }

            tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null)
            {
                return false;
            }

            var locale = LocalizationSettings.AvailableLocales?.GetLocale(new LocaleIdentifier(localeCode));
            if (locale == null)
            {
                return false;
            }

            table = tableCollection.GetTable(locale.Identifier) as AssetTable;
            return table != null;
        }

        private AudioClip TryGetClipFromTable(string localeCode)
        {
            if (!TryGetAssetTableAndEntry(localeCode, out _, out var table, out string entryKey))
            {
                return null;
            }

            var entry = table.GetEntry(entryKey);
            if (entry == null || string.IsNullOrEmpty(entry.Guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(entry.Guid));
        }

        private void TrySetClipInTable(string localeCode, AudioClip clip)
        {
            if (!TryGetAssetTableAndEntry(localeCode, out var tableCollection, out var table, out string entryKey))
            {
                return;
            }

            string guid = clip != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip)) : string.Empty;
            var entry = table.GetEntry(entryKey);
            if (entry == null)
            {
                tableCollection.AddAssetToTable(table, entryKey, clip);
            }
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
