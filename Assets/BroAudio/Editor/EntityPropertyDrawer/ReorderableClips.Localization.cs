#if PACKAGE_LOCALIZATION
using System.Collections.Generic;
using UnityEditor;
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

        private SerializedProperty _localizationTableProp;
        private SerializedProperty _localizationEntryProp;

        private void InitLocalization(UnityEditor.SerializedObject serializedObject)
        {
            _localizationTableProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationTable);
            _localizationEntryProp = serializedObject.FindProperty(AudioEntity.LocalizationEditorPropertyName.LocalizationEntry);
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void DisposeLocalization()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale locale)
        {
            _entity.Update();
        }

        private void DrawLocalizationHeader(Rect rect)
        {
            HandleClipsDragAndDrop(rect);

            Rect labelRect = new Rect(rect) { width = HeaderLabelWidth };
            Rect valueRect = new Rect(rect) { width = MulticlipsValueLabelWidth, x = rect.xMax - MulticlipsValueLabelWidth };
            Rect remainRect = new Rect(rect) { width = rect.width - HeaderLabelWidth - MulticlipsValueLabelWidth, x = labelRect.xMax };
            EditorScriptingExtension.SplitRectHorizontal(remainRect, 0.5f, 10f, out var multiclipOptionRect, out var masterVolRect);

            EditorGUI.LabelField(labelRect, "Clips");

            var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
            playMode = (MulticlipsPlayMode)EditorGUI.EnumPopup(multiclipOptionRect, playMode);
            _playModeProp.enumValueIndex = (int)playMode;

            DrawMasterVolume(masterVolRect);

            EditorGUI.LabelField(valueRect, "Locale", GUIStyleHelper.MiddleCenterText);
            EditorGUI.LabelField(multiclipOptionRect.DissolveHorizontal(0.5f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.MiddleCenterRichText);
        }

        public void DrawLocalizationTableDropdowns(Rect rect)
        {
            float halfWidth = (rect.width - Gap) * 0.5f;
            Rect tableRect = new Rect(rect) { width = halfWidth };
            Rect entryRect = new Rect(rect) { x = tableRect.xMax + Gap, width = halfWidth };

            if (_localizationTableProp != null)
                DrawAssetTableDropdown(tableRect);
            if (_localizationEntryProp != null)
                DrawTableEntryDropdown(entryRect);
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
                // Clear entry when table changes
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

        private void DrawLocalizationElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var property = _reorderableList.serializedProperty;
            var clipProp = property.GetArrayElementAtIndex(index);
            var localeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Locale));
            var volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));

            string localeCode = localeProp?.FindPropertyRelative("m_Code")?.stringValue ?? string.Empty;

            Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (_reorderableList.elementHeight - PlayButtonSize.y) * 0.5f;

            Rect localeRect = new Rect(rect) { x = buttonRect.xMax + Gap, width = LocaleLabelWidth };

            float usedWidth = buttonRect.width + Gap + LocaleLabelWidth + Gap;
            float remaining = rect.width - usedWidth;
            Rect clipRect = new Rect(rect) { x = localeRect.xMax + Gap, width = remaining * 0.55f };

            Rect volLabelRect = new Rect(rect) { x = clipRect.xMax + Gap, width = SliderLabelWidth };
            Rect volSliderRect = new Rect(rect)
            {
                x = volLabelRect.xMax,
                width = rect.xMax - volLabelRect.xMax,
                y = rect.y + 2f
            };

            // Locale label (read-only)
            EditorGUI.LabelField(localeRect, string.IsNullOrEmpty(localeCode) ? "(no locale)" : localeCode);

            // Resolve AudioClip from table for this locale
            AudioClip currentClip = TryGetClipFromTable(localeCode);

            // Play button
            if (currentClip != null)
            {
                bool isPlaying = string.Equals(_currentPlayingClipPath, clipProp.propertyPath);
                var image = GetPlaybackButtonIcon(isPlaying).image;
                var buttonContent = new GUIContent(image, EditorAudioPreviewer.IgnoreSettingTooltip);
                if (GUI.Button(buttonRect, buttonContent))
                {
                    if (isPlaying)
                        EditorAudioPreviewer.Instance.StopAllClips();
                    else
                        PreviewLocalizationClip(currentClip, clipProp, volProp);
                }
            }

            // AudioClip field — live proxy to Asset Table
            EditorGUI.BeginChangeCheck();
            var newClip = EditorGUI.ObjectField(clipRect, currentClip, typeof(AudioClip), false) as AudioClip;
            if (EditorGUI.EndChangeCheck())
            {
                TrySetClipInTable(localeCode, newClip);
            }

            // Volume slider
            EditorGUI.LabelField(volLabelRect, EditorGUIUtility.IconContent(IconConstant.AudioSpeakerOn));
            float newVol = BroEditorUtility.DrawVolumeSlider(volSliderRect, volProp.floatValue, out bool hasChanged, out float newSliderValue);
            if (hasChanged)
            {
                volProp.floatValue = newVol;
            }
            DrawDecibelValuePeeking(volProp.floatValue, 3f, volSliderRect, newSliderValue);
        }

        private void DrawLocalizationAddLocaleMenu()
        {
            var menu = new GenericMenu();
            var availableLocales = LocalizationSettings.AvailableLocales?.Locales;
            if (availableLocales != null)
            {
                var existing = new HashSet<string>();
                var prop = _reorderableList.serializedProperty;
                for (int i = 0; i < _reorderableList.count; i++)
                {
                    var clipProp = prop.GetArrayElementAtIndex(i);
                    string code = clipProp.FindPropertyRelative(nameof(BroAudioClip.Locale))
                                         ?.FindPropertyRelative("m_Code")?.stringValue ?? string.Empty;
                    if (!string.IsNullOrEmpty(code))
                        existing.Add(code);
                }

                foreach (var locale in availableLocales)
                {
                    string code = locale.Identifier.Code;
                    if (!existing.Contains(code))
                    {
                        var captured = locale;
                        menu.AddItem(new GUIContent($"{locale.name} ({code})"), false, () => AddLocaleClip(captured));
                    }
                }
            }

            if (menu.GetItemCount() == 0)
                menu.AddDisabledItem(new GUIContent("No more locales available"));

            menu.ShowAsContext();
        }

        private void AddLocaleClip(Locale locale)
        {
            var prop = _reorderableList.serializedProperty;
            prop.arraySize++;
            var newClipProp = prop.GetArrayElementAtIndex(prop.arraySize - 1);
            ResetBroAudioClipSerializedProperties(newClipProp);

            var localeCodeProp = newClipProp.FindPropertyRelative(nameof(BroAudioClip.Locale))
                                            ?.FindPropertyRelative("m_Code");
            if (localeCodeProp != null)
                localeCodeProp.stringValue = locale.Identifier.Code;

            prop.serializedObject.ApplyModifiedProperties();
            _reorderableList.index = prop.arraySize - 1;
        }

        private void PreviewLocalizationClip(AudioClip audioClip, SerializedProperty clipProp, SerializedProperty volProp)
        {
            var currentEvent = Event.current;
            var transport = new SerializedTransport(clipProp, audioClip.length);
            var req = currentEvent.CreatePreviewRequest(audioClip, volProp.floatValue, transport);
            GetBaseAndRandomValue(RandomFlag.Volume, _entity, out req.BaseMasterVolume, out req.MasterVolume);
            GetBaseAndRandomValue(RandomFlag.Pitch, _entity, out req.BasePitch, out req.Pitch);

            _onRequestClipPreview?.Invoke(clipProp.propertyPath, req);
            _currentPlayingClipPath = clipProp.propertyPath;
            EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(PreviewRect, req);
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
