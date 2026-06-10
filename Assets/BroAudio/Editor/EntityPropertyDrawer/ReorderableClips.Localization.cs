#if PACKAGE_LOCALIZATION
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
    public partial class ReorderableClips
    {

        // Unity Localization serialized property field names
        private const string TableCollectionNameField = "m_TableCollectionName";
        private const string EntryKeyField = "m_Key";
        private const string EntryKeyIdField = "m_KeyId";
        private const string LocaleCodeField = "m_Code";
        private const float DropdownLabelWidth = 70f;

        /// <summary>
        /// Uses the editor API to get locales synchronously from the Addressable settings.
        /// The runtime API (LocalizationSettings.AvailableLocales.Locales) uses a non-serialized list
        /// that is only populated via async loading in play mode, so it is empty after domain reload.
        /// </summary>
        private static IList<Locale> EditorAvailableLocales => LocalizationEditorSettings.GetLocales();

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

        private void CheckLocalizationMode(MulticlipsPlayMode previousMode, ref MulticlipsPlayMode newMode)
        {
            if (newMode != MulticlipsPlayMode.Localization)
            {
                return;
            }

            bool hasChanged = previousMode != newMode;
            bool hasAnyClip = HasAnyAudioClip || HasAnyAddressableClip;
            if (hasChanged && hasAnyClip && !ConfirmSwitchToLocalizationMode())
            {
                newMode = previousMode;
            }
        }

        private bool ConfirmSwitchToLocalizationMode()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Switch to Localization Mode",
                "Switching to Localization mode will clear all AudioClip references and clip properties on this entity, and force-enable Addressables for this entity. Continue?",
                "Yes",
                "No");

            if (confirmed)
            {
                var clipsProp = _reorderableList.serializedProperty;
                for (int i = 0; i < clipsProp.arraySize; i++)
                {
                    ResetBroAudioClipSerializedProperties(clipsProp.GetArrayElementAtIndex(i));
                }

                var useAddressablesProp = clipsProp.serializedObject.FindProperty(nameof(AudioEntity.UseAddressables));
                if (useAddressablesProp != null)
                {
                    useAddressablesProp.boolValue = true;
                }

                clipsProp.serializedObject.ApplyModifiedProperties();
            }
            return confirmed;
        }

        private void InitLocalization(SerializedObject serializedObject)
        {
            _localizationTableProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.Table);
            _localizationEntryProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.Entry);

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
            int targetCount = 1 + (EditorAvailableLocales?.Count ?? 0);
            if (_localizationListData.Count == targetCount)
            {
                return;
            }

            while (_localizationListData.Count < targetCount)
            {
                _localizationListData.Add(0);
            }

            if (_localizationListData.Count > targetCount)
            {
                _localizationListData.RemoveRange(targetCount, _localizationListData.Count - targetCount);
            }

            SyncClipsWithLocales();
        }

        private bool IsClipsSyncedWithLocales()
        {
            var availableLocales = EditorAvailableLocales;
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

        private struct SavedClipProperties
        {
            public float Volume;
            public float FadeIn;
            public float FadeOut;
            public float StartPosition;
            public float EndPosition;
            public float Delay;
            public int Weight;
        }

        private void SyncClipsWithLocales()
        {
            var availableLocales = EditorAvailableLocales;
            if (availableLocales == null || availableLocales.Count == 0)
            {
                return;
            }

            if (IsClipsSyncedWithLocales())
            {
                return;
            }

            var clipsProp = _reorderableList.serializedProperty;

            // Preserve all per-clip properties by locale code before resizing.
            var savedProps = new Dictionary<string, SavedClipProperties>();
            for (int i = 0; i < clipsProp.arraySize; i++)
            {
                var existing = clipsProp.GetArrayElementAtIndex(i);
                string code = existing.FindPropertyRelative(nameof(BroAudioClip.Locale))?.FindPropertyRelative(LocaleCodeField)?.stringValue ?? string.Empty;
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                savedProps[code] = new SavedClipProperties
                {
                    Volume = existing.FindPropertyRelative(nameof(BroAudioClip.Volume))?.floatValue ?? AudioConstant.FullVolume,
                    FadeIn = existing.FindPropertyRelative(nameof(BroAudioClip.FadeIn))?.floatValue ?? 0f,
                    FadeOut = existing.FindPropertyRelative(nameof(BroAudioClip.FadeOut))?.floatValue ?? 0f,
                    StartPosition = existing.FindPropertyRelative(nameof(BroAudioClip.StartPosition))?.floatValue ?? 0f,
                    EndPosition = existing.FindPropertyRelative(nameof(BroAudioClip.EndPosition))?.floatValue ?? 0f,
                    Delay = existing.FindPropertyRelative(nameof(BroAudioClip.Delay))?.floatValue ?? 0f,
                    Weight = existing.FindPropertyRelative(nameof(BroAudioClip.Weight))?.intValue ?? 0,
                };
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

                bool hasSaved = savedProps.TryGetValue(code, out var saved);
                SetFloatProp(clipProp, nameof(BroAudioClip.Volume), hasSaved ? saved.Volume : AudioConstant.FullVolume);
                SetFloatProp(clipProp, nameof(BroAudioClip.FadeIn), hasSaved ? saved.FadeIn : 0f);
                SetFloatProp(clipProp, nameof(BroAudioClip.FadeOut), hasSaved ? saved.FadeOut : 0f);
                SetFloatProp(clipProp, nameof(BroAudioClip.StartPosition), hasSaved ? saved.StartPosition : 0f);
                SetFloatProp(clipProp, nameof(BroAudioClip.EndPosition), hasSaved ? saved.EndPosition : 0f);
                SetFloatProp(clipProp, nameof(BroAudioClip.Delay), hasSaved ? saved.Delay : 0f);
                SetIntProp(clipProp, nameof(BroAudioClip.Weight), hasSaved ? saved.Weight : 0);
            }

            _entity.ApplyModifiedProperties();

            static void SetFloatProp(SerializedProperty parent, string name, float value)
            {
                var prop = parent.FindPropertyRelative(name);
                if (prop != null)
                {
                    prop.floatValue = value;
                }
            }

            static void SetIntProp(SerializedProperty parent, string name, int value)
            {
                var prop = parent.FindPropertyRelative(name);
                if (prop != null)
                {
                    prop.intValue = value;
                }
            }
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
            Rect openWindowButtonRect = new Rect(rect) { size = PlayButtonSize};
            openWindowButtonRect.y += (rect.height - PlayButtonSize.y) * 0.5f - 3f;

            if (GUI.Button(openWindowButtonRect, new GUIContent(LocalizationTablesWindowIcon, "Open Localization Tables")))
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Localization Tables");
            }

            float remaining = rect.width - PlayButtonSize.x - Gap;
            float halfWidth = (remaining - Gap) * 0.5f;
            Rect tableRect = new Rect(rect) { x = openWindowButtonRect.xMax + Gap, width = halfWidth };
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
            var availableLocales = EditorAvailableLocales;
            if (availableLocales == null || localeIndex >= availableLocales.Count)
            {
                return;
            }

            var locale = availableLocales[localeIndex];
            string localeCode = locale.Identifier.Code;
            AudioClip currentClip = TryGetClipFromTable(localeCode);

            Rect localeRect = new Rect(rect) { width = LocaleLabelWidth, x = rect.xMax - LocaleLabelWidth };

            Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (GetLocalizationElementHeight(localeIndex + 1) - PlayButtonSize.y) * 0.5f;

            float remainWidth = rect.width - buttonRect.width - localeRect.width;
            Rect clipRect = new Rect(rect) { x = buttonRect.xMax + Gap, width = (remainWidth * ObjectPickerRatio) - Gap };

            Rect volIconRect = new Rect(rect) { x = clipRect.xMax + Gap, width = SliderLabelWidth };
            Rect volumeRect = new Rect(rect) { x = volIconRect.xMax, width = (remainWidth * (1 - ObjectPickerRatio)) - Gap - SliderLabelWidth };

            var clipsProp = _reorderableList.serializedProperty;
            SerializedProperty localeClipProp = localeIndex < clipsProp.arraySize
                ? clipsProp.GetArrayElementAtIndex(localeIndex)
                : null;

            if (currentClip != null)
            {
                string clipPath = localeClipProp?.propertyPath;
                bool isPlaying = clipPath != null && string.Equals(_currentPlayingClipPath, clipPath);
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
                        PreviewLocalizationClip(currentClip, clipPath, localeClipProp);
                    }
                }
            }

            HandleLocalizationClipDragAndDrop(clipRect, localeCode);

            EditorGUI.BeginChangeCheck();
            var newClip = EditorGUI.ObjectField(clipRect, currentClip, typeof(AudioClip), false) as AudioClip;
            if (EditorGUI.EndChangeCheck())
            {
                TrySetClipInTable(localeCode, newClip);
            }

            var volumeProp = localeClipProp?.FindPropertyRelative(nameof(BroAudioClip.Volume));
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

            EditorGUI.LabelField(localeRect, string.IsNullOrEmpty(localeCode) ? "(no locale)" : localeCode, GUIStyleHelper.MiddleCenterText);
        }

        private float GetLocalizationElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 4f;
        }

        private void HandleLocalizationClipDragAndDrop(Rect dropRect, string localeCode)
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
            {
                return;
            }

            if (!dropRect.Contains(evt.mousePosition))
            {
                return;
            }

            AudioClip dragged = null;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is AudioClip clip)
                {
                    dragged = clip;
                    break;
                }
            }

            if (dragged == null)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                TrySetClipInTable(localeCode, dragged);
                evt.Use();
            }
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

            var locales = EditorAvailableLocales;
            var selectedLocale = LocalizationSettings.SelectedLocale
                ?? LocalizationSettings.ProjectLocale;
            if (selectedLocale == null && locales != null && locales.Count > 0)
            {
                selectedLocale = locales[0];
            }

            if (selectedLocale == null)
            {
                return false;
            }

            string localeCode = selectedLocale.Identifier.Code;
            audioClip = TryGetClipFromTable(localeCode);
            if (audioClip == null)
            {
                return false;
            }

            int found = FindClipIndexByLocaleCode(localeCode);
            if (found >= 0)
            {
                clipIndex = found;
            }

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
                {
                    return i;
                }
            }
            return -1;
        }

        private bool TryGetLocalizationSelectedClip(out AudioClip audioClip)
        {
            audioClip = null;
            int locIndex = _localizationList?.index ?? -1;
            int clipIndex = locIndex - 1;
            var availableLocales = EditorAvailableLocales;
            if (clipIndex >= 0 && availableLocales != null && clipIndex < availableLocales.Count)
            {
                string localeCode = availableLocales[clipIndex].Identifier.Code;
                audioClip = TryGetClipFromTable(localeCode);
            }
            return audioClip != null;
        }

        private void PreviewLocalizationClip(AudioClip audioClip, string previewPath, SerializedProperty clipProp)
        {
            var currentEvent = Event.current;
            PreviewRequest req;
            if (currentEvent.button == 0 && clipProp != null) // Left Click
            {
                var volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
                var transport = new SerializedTransport(clipProp, audioClip.length);
                req = currentEvent.CreatePreviewRequest(audioClip, volProp?.floatValue ?? AudioConstant.FullVolume, transport);
                GetBaseAndRandomValue(RandomFlag.Volume, _entity, out req.BaseMasterVolume, out req.MasterVolume);
                GetBaseAndRandomValue(RandomFlag.Pitch, _entity, out req.BasePitch, out req.Pitch);
            }
            else
            {
                req = currentEvent.CreatePreviewRequest(audioClip);
            }

            _onRequestClipPreview?.Invoke(previewPath, req);
            EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(PreviewRect, req);
        }

        private static readonly GUIContent[] NoneOnlyLabels = { new GUIContent("None") };

        private static Texture2D _localizationTablesWindowIcon;
        private static Texture2D LocalizationTablesWindowIcon
        {
            get
            {
                if (_localizationTablesWindowIcon == null)
                {
                    _localizationTablesWindowIcon = EditorGUIUtility.Load(
                        "Packages/com.unity.localization/Editor/Icons/Localization Tables Window/LocalizationTablesWindow On.png") as Texture2D;
                }
                return _localizationTablesWindowIcon;
            }
        }

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

            int newIndex = 0;
            using (new EditorScriptingExtension.LabelWidthScope(DropdownLabelWidth))
            {
                newIndex = EditorGUI.Popup(rect, new GUIContent("Asset Table"), selectedIndex, _cachedTableLabels);
            }

            if (newIndex != selectedIndex)
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
                DrawPopup(rect, 0, NoneOnlyLabels);
                return;
            }

            var tableCollection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
            if (tableCollection == null || tableCollection.SharedData == null)
            {
                DrawPopup(rect, 0, NoneOnlyLabels);
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

            int newIndex = DrawPopup(rect, selectedIndex, _cachedEntryLabels);
            if (newIndex != selectedIndex)
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

            int DrawPopup(Rect rect, int index, GUIContent[] labels)
            {
                int newIndex;
                using (new EditorScriptingExtension.LabelWidthScope(DropdownLabelWidth))
                {
                    newIndex = EditorGUI.Popup(rect, new GUIContent("Table Entry"), index, labels);
                }
                return newIndex;
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

            var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(localeCode));
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

            var entry = table.GetEntry(entryKey);
            if (entry != null && !string.IsNullOrEmpty(entry.Guid))
            {
                tableCollection.RemoveAssetFromTable(table, entryKey, createUndo: true);
            }

            if (clip != null)
            {
                tableCollection.AddAssetToTable(table, entryKey, clip, createUndo: true);
            }

            EditorUtility.SetDirty(table);
            if (tableCollection.SharedData != null)
            {
                EditorUtility.SetDirty(tableCollection.SharedData);
            }

            // Like DrawObjectPicker: reset playback settings and invalidate the cached transport on reassign,
            // else the previous clip's Volume/Start/End/Fade persist and clamping keeps the old clip length.
            int clipIndex = FindClipIndexByLocaleCode(localeCode);
            if (clipIndex >= 0 && clipIndex < _reorderableList.serializedProperty.arraySize)
            {
                var clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(clipIndex);
                ResetBroClipPlaybackSetting(clipProp);
                _entity.ApplyModifiedProperties();
                OnClipChanged?.Invoke(clipProp.propertyPath);
            }
        }
    }
}
#endif