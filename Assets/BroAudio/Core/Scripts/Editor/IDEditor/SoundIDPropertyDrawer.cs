using UnityEngine;
using UnityEditor;
using Ami.Extension;
using UnityEditor.IMGUI.Controls;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
    [CustomPropertyDrawer(typeof(SoundID))]
    public class SoundIDPropertyDrawer : PropertyDrawer
    {
        public const string DefaultIDName = "None";
        public const string IDMissing = "Missing";
        public const string ToolTip = "refering to an AudioEntity";

        private readonly string _missingMessage = IDMissing.ToBold().ToItalics().SetColor(new Color(1f, 0.3f, 0.3f));
        private bool _isInit = false;
        private string _entityName = null;
        private bool _isPlaying = false;
        private EditorWindow _currentWindow = null;

        private readonly GUIStyle _dropdownStyle = new GUIStyle(EditorStyles.popup) { richText = true };
        private readonly GUIContent _libraryShortcut = 
            new GUIContent(EditorGUIUtility.IconContent(IconConstant.LibraryManagerShortcut)) { tooltip = "Open in Library Manager"};

        private float ButtonWidth => EditorGUIUtility.singleLineHeight * 1.5f;

        private void Init(SerializedProperty idProp,SerializedProperty assetProp)
        {
            _isInit = true;

            if (idProp.intValue == 0)
            {
                _entityName = DefaultIDName;
                return;
            }
            else if (idProp.intValue < 0)
            {
                _entityName = _missingMessage;
                return;
            }

            BroAudioType audioType = Utility.GetAudioType(idProp.intValue);
            if (!audioType.IsConcrete())
            {
                SetToMissing();
                return;
            }

            AudioAsset asset = assetProp.objectReferenceValue as AudioAsset;
            if (asset != null && BroEditorUtility.TryGetEntityName(asset,idProp.intValue,out _entityName))
            {
                return;
            }

            if(BroEditorUtility.TryGetCoreData(out var coreData))
            {
                foreach (var coreAsset in coreData.Assets)
                {
                    asset = coreAsset;
                    if (asset != null && BroEditorUtility.TryGetEntityName(asset, idProp.intValue, out _entityName))
                    {
                        assetProp.objectReferenceValue = asset;
                        assetProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        return;
                    }
                }
            }
            
            SetToMissing();

            void SetToMissing()
            {
                idProp.intValue = -1;
                _entityName = _missingMessage;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty idProp = property.FindPropertyRelative(nameof(SoundID.ID));
            SerializedProperty assetProp = property.FindPropertyRelative(SoundID.NameOf.SourceAsset);

            if (!_isInit)
            {
                Init(idProp, assetProp);
            }

            Rect suffixRect = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName, ToolTip));
            Rect dropdownRect = new Rect(suffixRect) { width = suffixRect.width - (ButtonWidth * 2) };
            Rect playbackButtonRect = new Rect(suffixRect) { width = ButtonWidth, x = dropdownRect.xMax };
            Rect libraryShortcutRect = new Rect(suffixRect) { width = ButtonWidth, x = playbackButtonRect.xMax };

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(_entityName, ToolTip), FocusType.Keyboard, _dropdownStyle))
            {
                var dropdown = new SoundIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
                dropdown.Show(dropdownRect);
            }

            using (new EditorGUI.DisabledScope(idProp.intValue <= 0))
            {
                IAudioAsset audioAsset = assetProp.objectReferenceValue as IAudioAsset;
                DrawAudioTypeLabel(dropdownRect, idProp, audioAsset);
                DrawPlaybackButton(playbackButtonRect, idProp.intValue, assetProp);
                DrawLibraryShortcutButton(libraryShortcutRect, idProp, audioAsset);
            }

            void OnSelect(int id, string name, ScriptableObject asset)
            {
                idProp.intValue = id;
                _entityName = name;
                assetProp.objectReferenceValue = asset;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawAudioTypeLabel(Rect dropdownRect, SerializedProperty idProp, IAudioAsset audioAsset)
        {
            if (BroEditorUtility.EditorSetting.ShowAudioTypeOnSoundID && audioAsset != null)
            {
                BroAudioType audioType = Utility.GetAudioType(idProp.intValue);
                Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(dropdownRect, 0.7f);
                EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(audioType));
                EditorGUI.LabelField(audioTypeRect, audioType.ToString(), GUIStyleHelper.MiddleCenterText);
            }
        }

        private void DrawPlaybackButton(Rect playbackButtonRect, int id, SerializedProperty assetProp)
        {
            if (GUI.Button(playbackButtonRect, GetPlaybackButtonIcon()))
            {
                if(_isPlaying)
                {
                    EditorPlayAudioClip.Instance.StopAllClips();
                    _isPlaying = false;
                    return;
                }

                object targetValue = fieldInfo.GetValue(assetProp.serializedObject.targetObject);
                if (targetValue is SoundID sound
                    && SoundID.TryGetAsset(sound, out var asset) && TryGetEntity(asset, out var entity))
                {
                    var data = new EditorPlayAudioClip.Data(entity.PickNewClip());
                    EditorPlayAudioClip.Instance.PlayClipByAudioSource(data, false, null, entity.GetPitch());
                    EditorPlayAudioClip.Instance.OnFinished = () => _isPlaying = false; ;
                    _isPlaying = true;

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

        private void OnPreviewAudioUpdate()
        {
            if(IsWindowFocusChanged())
            {
                EditorApplication.update -= OnPreviewAudioUpdate;
                EditorPlayAudioClip.Instance.StopAllClips();
                _isPlaying = false;
            }
        }

        private bool IsWindowFocusChanged()
        {
            EditorWindow latestWindow = EditorWindow.focusedWindow;
            bool isChanged = _currentWindow != null && _currentWindow != latestWindow;
            _currentWindow = latestWindow;
            return isChanged;
        }

        private void DrawLibraryShortcutButton(Rect libraryShortcutRect, SerializedProperty idProp, IAudioAsset audioAsset)
        {
            if (GUI.Button(libraryShortcutRect, _libraryShortcut))
            {
                LibraryManagerWindow.ShowWindowAndLocateToEntity(audioAsset.AssetGUID, idProp.intValue);
            }
        }

        private GUIContent GetPlaybackButtonIcon()
        {
            string icon = _isPlaying ? IconConstant.StopButton : IconConstant.PlayButton;
            return EditorGUIUtility.IconContent(icon);
        }
    } 
}
