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
        private const string DefaultIDName = "None";
        private const string EntityTooltip = "Identifier that points to a specific AudioEntity";
        private const float AudioTypeDissolveRatio = 0.65f;

        private AudioEntity _currentPlaying = null;
        private EditorWindow _currentWindow = null;

        private GUIStyle _dropdownStyle;
        private readonly GUIContent _libraryShortcut = 
            new GUIContent(EditorGUIUtility.IconContent(IconConstant.LibraryManagerShortcut)) { tooltip = "Open in Library Manager"};

        private float ButtonWidth => EditorGUIUtility.singleLineHeight * 1.5f;

        private AudioEntity GetEntity(SerializedProperty property)
        {
            var entityProp = property.FindPropertyRelative(SoundID.NameOf.Entity);

            if (entityProp.objectReferenceValue != null)
            {
                return entityProp.objectReferenceValue as AudioEntity;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            var idProp = property.FindPropertyRelative(SoundID.NameOf.ID);

            if (idProp.intValue == 0 || idProp.intValue == -1)
            {
                return null;
            }

            // can we convert it?
            if (!BroAudio.TryConvertIdToEntity(idProp.intValue, out var entity))
            {
                return null;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            entityProp.objectReferenceValue = entity;
            //idProp.intValue = 0;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return entityProp.objectReferenceValue as AudioEntity;
        }

        private string GetEntityName(SerializedProperty property)
        {
            var entity = GetEntity(property);

            if(entity == null)
            {
                return DefaultIDName;
            }

            return entity.Name;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _dropdownStyle ??= new GUIStyle(EditorStyles.popup) { richText = true };

            var entity = GetEntity(property);
            CacheDebugObject(property);

            Rect suffixRect = EditorGUI.PrefixLabel(position, label);
            Rect dropdownRect = new Rect(suffixRect) { width = suffixRect.width - (ButtonWidth * 2)};
            Rect playbackButtonRect = new Rect(suffixRect) { width = ButtonWidth, x = dropdownRect.xMax };
            Rect libraryShortcutRect = new Rect(suffixRect) { width = ButtonWidth, x = playbackButtonRect.xMax };

            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(GetEntityName(property), EntityTooltip), FocusType.Keyboard, _dropdownStyle))
            {
                var dropdown = new SoundIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
                dropdown.Show(dropdownRect);
            }

            DrawAudioTypeLabel(dropdownRect, entity);
            using (new EditorGUI.DisabledScope(entity == null))
            {                
                DrawPlaybackButton(playbackButtonRect, entity);
            }
            DrawLibraryShortcutButton(libraryShortcutRect, entity);

            void OnSelect(AudioEntity entity)
            {
                property.FindPropertyRelative(SoundID.NameOf.Entity).objectReferenceValue = entity;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void CacheDebugObject(SerializedProperty property)
        {
            SerializedProperty debugObjectProp = property.FindBackingFieldProperty(nameof(SoundID.DebugObject));
            if (property.serializedObject.targetObject != debugObjectProp.objectReferenceValue &&
                property.serializedObject.targetObject is MonoBehaviour mono)
            {
                debugObjectProp.objectReferenceValue = mono.gameObject;
                debugObjectProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void DrawAudioTypeLabel(Rect dropdownRect, AudioEntity entity)
        {
            if (BroEditorUtility.EditorSetting.ShowAudioTypeOnSoundID && entity != null)
            {
                dropdownRect = dropdownRect.PolarCoordinates(-2f);
                BroAudioType audioType = entity.AudioType;
                Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(dropdownRect, AudioTypeDissolveRatio);
                EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(audioType));
                EditorGUI.LabelField(audioTypeRect, audioType.ToString(), GUIStyleHelper.MiddleCenterText);
            }
        }

        private void DrawPlaybackButton(Rect playbackButtonRect, AudioEntity entity)
        {
            if (GUI.Button(playbackButtonRect, GetPlaybackButtonIcon(entity)))
            {
                if(_currentPlaying == entity)
                {
                    EditorAudioPreviewer.Instance.StopAllClips();
                    _currentPlaying = null;
                    return;
                }

                if (entity != null)
                {
                    var req = Event.current.CreatePreviewRequest(entity.PickNewClip());
                    req.MasterVolume = entity.GetMasterVolume();
                    req.BaseMasterVolume = entity.MasterVolume;
                    req.Pitch = entity.GetPitch();
                    req.Pitch = entity.Pitch;
                    EditorAudioPreviewer.Instance.Play(req);
                    EditorAudioPreviewer.Instance.OnFinished = OnPreviewAudioFinished;
                    _currentPlaying = entity;

                    EditorApplication.update += OnPreviewAudioUpdate;
                }
            }
        }

        private void OnPreviewAudioFinished()
        {
            _currentPlaying = null;
            EditorApplication.update -= OnPreviewAudioUpdate;
        }

        private void OnPreviewAudioUpdate()
        {
            EditorAudioPreviewer.Instance.UpdatePreview();
            if(IsWindowFocusChanged())
            {
                EditorApplication.update -= OnPreviewAudioUpdate;
                EditorAudioPreviewer.Instance.StopAllClips();
                _currentPlaying = null;
            }
        }

        private bool IsWindowFocusChanged()
        {
            EditorWindow latestWindow = EditorWindow.focusedWindow;
            bool isChanged = _currentWindow != null && _currentWindow != latestWindow;
            _currentWindow = latestWindow;
            return isChanged;
        }

        private void DrawLibraryShortcutButton(Rect libraryShortcutRect, AudioEntity entity)
        {
            if (GUI.Button(libraryShortcutRect, _libraryShortcut))
            {
                if(entity != null)
                {
                    LibraryManagerWindow.ShowWindowAndLocateToEntity(entity.AudioAsset, entity);
                }
                else
                {
                    LibraryManagerWindow.ShowWindow();
                }
            }
        }

        private GUIContent GetPlaybackButtonIcon(AudioEntity entity)
        {
            string icon = _currentPlaying == entity ? IconConstant.StopButton : IconConstant.PlayButton;
            return EditorGUIUtility.IconContent(icon);
        }
    } 
}
