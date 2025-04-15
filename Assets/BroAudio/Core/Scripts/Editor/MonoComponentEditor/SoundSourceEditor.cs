using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.SoundSource.NameOf;
using static Ami.Extension.EditorScriptingExtension;
using System.Collections.Generic;
using System;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundSource))]
    public class SoundSourceEditor : UnityEditor.Editor
    {
        public const float FadeOutFieldWidth = 70f;

        private RectOffset _inspectorPadding = null;
        private bool _isInit = false;
        private float _currentDrawedPosY = 0f;

        private GUIContent _playOnEnableContent = new GUIContent("Play On Enable", "Plays the sound whenever the GameObject is enabled");
        private GUIContent _onlyOnceContent = new GUIContent("Only Play Once", "Plays the sound only the first time the GameObject is enabled");
        private GUIContent _stopOnDisableContent = new GUIContent("Stop On Disable", "Stops the sound whenever the GameObject is disabled");
        private GUIContent _fadeOutContent = new GUIContent("Override Fade Out", "Overrides the fade-out time setting when stopping the sound on disable");
        private GUIContent _positionModeContent = new GUIContent("Position Mode", "Determines the sound's location when triggered.\n\n" +
            "[Global] Plays globally (2D), meaning the sound can be heard everywhere.\n\n" +
            "[Stay Here] Plays as a 3D sound and stays where the GameObject is located\n\n" +
            "[Follow Target] Plays as a 3D sound and follows the GameObject as it moves");
        private GUIContent _overrideGroupContent = new GUIContent("Override Playback Group", "Overrides the PlaybackGroup of the sound");
        private Dictionary<string, SerializedProperty> _mainPropertyDict = new Dictionary<string, SerializedProperty>();

        private void OnEnable()
        {
            _inspectorPadding = InspectorPadding;
            _isInit = true;
        }

        private SerializedProperty FindProperty(string path)
        {
            if (!_mainPropertyDict.TryGetValue(path, out var property))
            {
                property = serializedObject.FindProperty(path);
                _mainPropertyDict[path] = property;
            }
            return property;
        }

        public override void OnInspectorGUI()
        {
            if (!_isInit)
            {
                return;
            }
            SerializedProperty playOnEnableProp = FindProperty(PlayOnEnable);
            SerializedProperty stopOnDisableProp = FindProperty(StopOnDisable);
            SerializedProperty onlyOnceProp = FindProperty(OnlyPlayOnce);
            SerializedProperty fadeOutProp = FindProperty(OverrideFadeOut);
            SerializedProperty soundIDProp = FindProperty(SoundSource.NameOf.SoundID);
            SerializedProperty positionModeProp = FindProperty(PositionMode);
            SerializedProperty overrideGroupProp = FindProperty(OverrideGroup);

            _currentDrawedPosY = 1f;

            DrawBackgroudWindow(2, _inspectorPadding, ref _currentDrawedPosY);
            DrawBoldToggle(playOnEnableProp, _inspectorPadding, _playOnEnableContent);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledGroupScope(!playOnEnableProp.boolValue))
            {
                onlyOnceProp.boolValue = EditorGUILayout.Toggle(_onlyOnceContent, onlyOnceProp.boolValue);
            }

            EditorGUILayout.Space();
            _currentDrawedPosY += GUILayoutUtility.GetLastRect().height + EditorGUIUtility.standardVerticalSpacing;

            DrawBackgroudWindow(2, _inspectorPadding, ref _currentDrawedPosY);
            DrawBoldToggle(stopOnDisableProp, _inspectorPadding, _stopOnDisableContent);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledGroupScope(!stopOnDisableProp.boolValue))
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isOverrided = EditorGUILayout.Toggle(_fadeOutContent, fadeOutProp.floatValue >= 0f);
                
                EditorGUI.BeginDisabledGroup(!isOverrided);
                {
                    float tempFadeOut = Mathf.Max(fadeOutProp.floatValue, 0f);
                    tempFadeOut = EditorGUILayout.FloatField(GUIContent.none, tempFadeOut, GUILayout.Width(FadeOutFieldWidth));
                    fadeOutProp.floatValue = isOverrided && tempFadeOut >= 0f ? tempFadeOut : -1f;
                    EditorGUILayout.LabelField("sec");
                }
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(soundIDProp);
            EditorGUILayout.PropertyField(positionModeProp, _positionModeContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(overrideGroupProp, _overrideGroupContent);

            DrawOtherProperties();

            serializedObject.ApplyModifiedProperties();

            if(Application.isPlaying && target is SoundSource source)
            {
                EditorGUILayout.Space();
                AudioPlayer player = source.CurrentPlayer is AudioPlayerInstanceWrapper wrapper && wrapper.IsPlaying ? (AudioPlayer)wrapper : null;
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.ObjectField("Current Player", player, typeof(AudioPlayer), false);
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawOtherProperties()
        {
            var property = serializedObject.GetIterator();
            bool hasEntered = false;
            bool hasScriptChecked = false;

            while(property.NextVisible(!hasEntered))
            {
                hasEntered = true;

                if(!hasScriptChecked && property.name == "m_Script")
                {
                    hasScriptChecked = true;
                }
                else if(!_mainPropertyDict.ContainsKey(property.name))
                {
                    EditorGUILayout.PropertyField(property);
                }
            }
        }
    } 
}
