using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using System;
using static Ami.BroAudio.SoundVolume;
using static Ami.Extension.EditorScriptingExtension;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundVolume))]
    public class SoundVolumeEditor : UnityEditor.Editor
    {
        private const int SettingFieldCount = 3;

        private SerializedProperty _applyOnEnableProp = null;
        private SerializedProperty _resetOnDisableProp = null;
        private SerializedProperty _settingsProp = null;
        private SerializedProperty _sliderTypeProp = null;
        private SerializedProperty _allowBoostProp = null;

        private ReorderableList _settingsList = null;

        private readonly Rect[] _settingRects = new Rect[SettingFieldCount];
        private readonly float[] _settingRectRatio = new float[] { 0.325f, 0.325f, 0.35f };
        private readonly GUIContent _volumeGUIContent = new GUIContent("Volume");

        private void OnEnable()
        {
            _applyOnEnableProp = serializedObject.FindProperty(NameOf.ApplyOnEnable);
            _resetOnDisableProp = serializedObject.FindProperty(NameOf.ResetOnDisable);
            _settingsProp = serializedObject.FindProperty(NameOf.Settings);
            _sliderTypeProp = serializedObject.FindProperty(NameOf.SliderType);
            _allowBoostProp = serializedObject.FindProperty(NameOf.AllowBoost);

            _settingsList = new ReorderableList(serializedObject, _settingsProp)
            {
                drawHeaderCallback = OnDrawHeader,
                drawElementCallback = OnDrawelement,
                elementHeight = EditorGUIUtility.singleLineHeight * SettingFieldCount + ReorderableList.Defaults.padding,
                onAddCallback = OnAddElement,
            };
        }

        private void OnDrawelement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var property = _settingsProp.GetArrayElementAtIndex(index);
            var audioTypeProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.AudioType);
            var sliderProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.Slider);
            var volProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.Volume);

            rect.y += 2f;
            rect.height -= 4f;
            SplitRectVertical(rect, 2f, _settingRects, _settingRectRatio);
            EditorGUI.PropertyField(_settingRects[0], audioTypeProp);
            EditorGUI.PropertyField(_settingRects[1], sliderProp);

            Rect volRect = _settingRects[2];
            bool allowBoost = _allowBoostProp.boolValue && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL; 
            float maxVolume = allowBoost ? AudioConstant.MaxVolume : AudioConstant.FullVolume;
            volProp.floatValue = (SliderType)_sliderTypeProp.enumValueFlag switch
            {
                SliderType.Linear => EditorGUI.Slider(volRect, _volumeGUIContent, volProp.floatValue, 0f, maxVolume),
                SliderType.Logarithmic => DrawLogarithmicSlider(volRect, _volumeGUIContent, volProp.floatValue, maxVolume),
                SliderType.BroVolume => BroEditorUtility.DrawVolumeSlider(volRect, _volumeGUIContent, volProp.floatValue, allowBoost),
                _ => throw new NotImplementedException(),
            };

            static float DrawLogarithmicSlider(Rect rect, GUIContent content, float vol, float maxVolume)
            {
                Rect suffixRect = EditorGUI.PrefixLabel(rect, content);
                return DrawLogarithmicSlider_Horizontal(suffixRect, vol, AudioConstant.MinVolume, maxVolume);
            }
        }

        private void OnAddElement(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var elementProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
            var volProp = elementProp.FindPropertyRelative(SoundVolume.Setting.NameOf.Volume);
            volProp.floatValue = AudioConstant.FullVolume;
            elementProp.serializedObject.ApplyModifiedProperties();
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Volumes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_applyOnEnableProp);
            EditorGUILayout.PropertyField(_resetOnDisableProp);

            EditorGUILayout.LabelField("Slider", EditorStyles.boldLabel);
#if UNITY_WEBGL
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle(_allowBoostProp.displayName, false);
            EditorGUI.EndDisabledGroup();
#else
            EditorGUILayout.PropertyField(_allowBoostProp);
#endif
            DrawSliderTypeField();
            EditorGUILayout.Space();
            _settingsList.DoLayoutList();

            DrawSyncingButton();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSliderTypeField()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(_sliderTypeProp, GUILayout.Width(EditorGUIUtility.labelWidth + 200f));
                if (GUILayout.Button(EditorGUIUtility.IconContent(IconConstant.Help), EditorStyles.label))
                {
                    DevTool.SliderModelComparison.ShowWindow();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    Rect helpRect = GUILayoutUtility.GetLastRect();
                    EditorGUIUtility.AddCursorRect(helpRect, MouseCursor.Link);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSyncingButton()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayoutOption buttonHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f);
                var component = target as SoundVolume;
                if (GUILayout.Button("Read Volume From Slider", buttonHeight))
                {
                    component.ReadVolumeFromSlider();
                }
                if (GUILayout.Button("Set Volume To Slider", buttonHeight))
                {
                    component.SetVolumeToSlider();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}