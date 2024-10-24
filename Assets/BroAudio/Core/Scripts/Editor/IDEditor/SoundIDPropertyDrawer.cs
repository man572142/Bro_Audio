using UnityEngine;
using UnityEditor;
using Ami.Extension;
using UnityEditor.IMGUI.Controls;
using Ami.BroAudio.Data;
using System.Collections.Generic;

namespace Ami.BroAudio.Editor
{
    [CustomPropertyDrawer(typeof(SoundID))]
    public class SoundIDPropertyDrawer : PropertyDrawer
    {
        public const string DefaultIDName = "None";
        public const string IDMissing = "Missing";
        public const string ToolTip = "refering to an AudioEntity";

        private readonly string _missingMessage = IDMissing.ToBold().ToItalics().SetColor(new Color(1f, 0.3f, 0.3f));
        private int _currentPlayingID = 0;
        private EditorWindow _currentWindow = null;
        private Dictionary<int, string> _entityNameDict = new Dictionary<int, string>();

        private GUIStyle _dropdownStyle;
        private readonly GUIContent _libraryShortcut = 
            new GUIContent(EditorGUIUtility.IconContent(IconConstant.LibraryManagerShortcut)) { tooltip = "Open in Library Manager"};

        private float ButtonWidth => EditorGUIUtility.singleLineHeight * 1.5f;

        private string CacheEntityName(int id, SerializedProperty assetProp)
        {
            if(id == 0)
            {
                return DefaultIDName;
            }
            else if (id < 0)
            {
                return _missingMessage;
            }

            BroAudioType audioType = Utility.GetAudioType(id);
            if (!audioType.IsConcrete())
            {
                return _missingMessage;
            }

            AudioAsset asset = assetProp.objectReferenceValue as AudioAsset;
            string name;
            if (asset != null && BroEditorUtility.TryGetEntityName(asset,id,out name))
            {
                return name;
            }

            if(BroEditorUtility.TryGetCoreData(out var coreData))
            {
                foreach (var coreAsset in coreData.Assets)
                {
                    asset = coreAsset;
                    if (asset != null && BroEditorUtility.TryGetEntityName(asset, id, out name))
                    {
                        assetProp.objectReferenceValue = asset;
                        assetProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        return name;
                    }
                }
            }
            return _missingMessage;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _dropdownStyle ??= new GUIStyle(EditorStyles.popup) { richText = true };
            SerializedProperty idProp = property.FindPropertyRelative(nameof(SoundID.ID));
            SerializedProperty assetProp = property.FindPropertyRelative(SoundID.NameOf.SourceAsset);
            int id = idProp.intValue;

            if (!_entityNameDict.TryGetValue(id, out string entityName))
            {
                _entityNameDict[id] = CacheEntityName(id, assetProp);
            }

            Rect suffixRect = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName, ToolTip));
            Rect dropdownRect = new Rect(suffixRect) { width = suffixRect.width - (ButtonWidth * 2)};
            Rect playbackButtonRect = new Rect(suffixRect) { width = ButtonWidth, x = dropdownRect.xMax };
            Rect libraryShortcutRect = new Rect(suffixRect) { width = ButtonWidth, x = playbackButtonRect.xMax };

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(entityName, ToolTip), FocusType.Keyboard, _dropdownStyle))
            {
                var dropdown = new SoundIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
                dropdown.Show(dropdownRect);
            }

            IAudioAsset audioAsset = assetProp.objectReferenceValue as IAudioAsset;
            DrawAudioTypeLabel(dropdownRect, id, audioAsset);
            using (new EditorGUI.DisabledScope(id <= 0))
            {                
                DrawPlaybackButton(playbackButtonRect, id, assetProp);
            }
            DrawLibraryShortcutButton(libraryShortcutRect, id, audioAsset);

            void OnSelect(int id, string name, ScriptableObject asset)
            {
                idProp.intValue = id;
                _entityNameDict[id] = name;
                assetProp.objectReferenceValue = asset;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawAudioTypeLabel(Rect dropdownRect, int id, IAudioAsset audioAsset)
        {
            if (BroEditorUtility.EditorSetting.ShowAudioTypeOnSoundID && audioAsset != null)
            {
                dropdownRect = dropdownRect.PolarCoordinates(-2f);
                BroAudioType audioType = Utility.GetAudioType(id);
                Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(dropdownRect, 0.7f);
                EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(audioType));
                EditorGUI.LabelField(audioTypeRect, audioType.ToString(), GUIStyleHelper.MiddleCenterText);
            }
        }

        private void DrawPlaybackButton(Rect playbackButtonRect, int id, SerializedProperty assetProp)
        {
            if (GUI.Button(playbackButtonRect, GetPlaybackButtonIcon(id)))
            {
                if(_currentPlayingID == id)
                {
                    EditorPlayAudioClip.Instance.StopAllClips();
                    _currentPlayingID = 0;
                    return;
                }

                if (assetProp.objectReferenceValue is AudioAsset asset && TryGetEntity(asset, out var entity))
                {
                    var data = new EditorPlayAudioClip.Data(entity.PickNewClip());
                    EditorPlayAudioClip.Instance.PlayClipByAudioSource(data, false, null, entity.GetPitch());
                    EditorPlayAudioClip.Instance.OnFinished = OnPreviewAudioFinished;
                    _currentPlayingID = id;

                    EditorApplication.update += OnPreviewAudioUpdate;
                }
            }

            bool TryGetEntity(AudioAsset audioAsset, out AudioEntity audioEntity)
            {
                audioEntity = null;
                foreach (var entity in audioAsset.Entities)
                {
                    if (entity.ID == id)
                    {
                        audioEntity = entity;
                        return true;
                    }
                }
                return false;
            }
        }

        private void OnPreviewAudioFinished()
        {
            _currentPlayingID = 0;
            EditorApplication.update -= OnPreviewAudioUpdate;
        }

        private void OnPreviewAudioUpdate()
        {
            if(IsWindowFocusChanged())
            {
                EditorApplication.update -= OnPreviewAudioUpdate;
                EditorPlayAudioClip.Instance.StopAllClips();
                _currentPlayingID = 0;
            }
        }

        private bool IsWindowFocusChanged()
        {
            EditorWindow latestWindow = EditorWindow.focusedWindow;
            bool isChanged = _currentWindow != null && _currentWindow != latestWindow;
            _currentWindow = latestWindow;
            return isChanged;
        }

        private void DrawLibraryShortcutButton(Rect libraryShortcutRect, int id, IAudioAsset audioAsset)
        {
            if (GUI.Button(libraryShortcutRect, _libraryShortcut))
            {
                if(id > 0 && !string.IsNullOrEmpty(audioAsset.AssetGUID))
                {
                    LibraryManagerWindow.ShowWindowAndLocateToEntity(audioAsset.AssetGUID, id);
                }
                else
                {
                    LibraryManagerWindow.ShowWindow();
                }
            }
        }

        private GUIContent GetPlaybackButtonIcon(int id)
        {
            string icon = _currentPlayingID == id ? IconConstant.StopButton : IconConstant.PlayButton;
            return EditorGUIUtility.IconContent(icon);
        }
    } 
}
