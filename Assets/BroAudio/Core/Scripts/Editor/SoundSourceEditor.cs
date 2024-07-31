using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.BroAudio.SoundSource.NameOf;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundSource))]
    public class SoundSourceEditor : UnityEditor.Editor
    {
        public const float FadeOutFieldWidth = 70f;
        public const float WindowPaddingX = 10f;
        public const float WindowPaddingY = 4f;

        private SerializedProperty _playProp = null;
        private SerializedProperty _stopProp = null;
        private SerializedProperty _onlyOnceProp = null;
        private SerializedProperty _fadeOutProp = null;
        private SerializedProperty _soundIDProp = null;
        private SerializedProperty _positionModeProp = null;

        private RectOffset _inspectorPadding = null;

        private bool _isInit = false;
        private float _currentDrawedWindowY = 0f;

        private void OnEnable()
        {
            _playProp = FindProperty(PlayOnEnable);
            _stopProp = FindProperty(StopOnDisable);
            _onlyOnceProp = FindProperty(OnlyPlayOnce);
            _fadeOutProp = FindProperty(OverrideFadeOut);
            _soundIDProp = FindProperty(SoundSource.NameOf.SoundID);
            _positionModeProp = FindProperty(PositionMode);

            _inspectorPadding = EditorScriptingExtension.InspectorPadding;

            _isInit = true;

            SerializedProperty FindProperty(string path)
            {
                return serializedObject.FindProperty(path);
            }
        }

        public override void OnInspectorGUI()
        {
            if (!_isInit)
            {
                return;
            }
            _currentDrawedWindowY = 1f;

            DrawBackgroudWindow(2);
            DrawBoldToggle(ref _playProp);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledGroupScope(!_playProp.boolValue))
            {
                _onlyOnceProp.boolValue = EditorGUILayout.Toggle(_onlyOnceProp.displayName, _onlyOnceProp.boolValue);
            }

            EditorGUILayout.Space();
            _currentDrawedWindowY += EditorGUIUtility.standardVerticalSpacing * 2;

            DrawBackgroudWindow(2);
            DrawBoldToggle(ref _stopProp);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledGroupScope(!_stopProp.boolValue))
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isOverrided = EditorGUILayout.Toggle(_fadeOutProp.displayName, _fadeOutProp.floatValue >= 0f);
                
                EditorGUI.BeginDisabledGroup(!isOverrided);
                {
                    float tempFadeOut = Mathf.Max(_fadeOutProp.floatValue, 0f);
                    tempFadeOut = EditorGUILayout.FloatField(GUIContent.none, tempFadeOut, GUILayout.Width(FadeOutFieldWidth));
                    _fadeOutProp.floatValue = isOverrided && tempFadeOut >= 0f ? tempFadeOut : -1f;
                    EditorGUILayout.LabelField("sec");
                }
                EditorGUI.EndDisabledGroup();
            }


            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_soundIDProp);
            EditorGUILayout.PropertyField(_positionModeProp);

            serializedObject.ApplyModifiedProperties();

            if(Application.isPlaying && target is SoundSource source && 
                source.CurrentPlayer != null && source.CurrentPlayer is AudioPlayerInstanceWrapper wrapper)
            {
                AudioPlayer player = wrapper;
                EditorGUI.BeginDisabledGroup(true);
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.ObjectField("Current Player",player, typeof(AudioPlayer), false);
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawBoldToggle(ref SerializedProperty property)
        {
            // We can't draw a field with Bold Label because it's used for prefab overrides
            EditorGUILayout.LabelField(property.displayName, EditorStyles.boldLabel);
            Rect toggleRect = GUILayoutUtility.GetLastRect();
            toggleRect.x = EditorGUIUtility.labelWidth + _inspectorPadding.left + 2f;
            property.boolValue = EditorGUI.Toggle(toggleRect, property.boolValue);
        }

        private void DrawBackgroudWindow(int lineCount)
        {
            if (Event.current.type == EventType.Repaint)
            {
                float width = EditorGUIUtility.currentViewWidth - (WindowPaddingX * 2);
                float height = (EditorGUIUtility.singleLineHeight * lineCount) + (WindowPaddingY * 2);
                GUIStyle style = new GUIStyle("AnimationKeyframeBackground");
                style.Draw(new Rect(WindowPaddingX, _currentDrawedWindowY, width, height), false, false, false, false);
                _currentDrawedWindowY += height;
            }
        }
    } 
}
