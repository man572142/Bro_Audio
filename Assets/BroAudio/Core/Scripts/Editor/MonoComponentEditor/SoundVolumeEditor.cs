using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using System;
using static Ami.BroAudio.SoundVolume;
using static Ami.Extension.EditorScriptingExtension;
using Ami.BroAudio.Tools;
using Ami.BroAudio.Editor.Setting;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundVolume))]
    public class SoundVolumeEditor : UnityEditor.Editor
    {
        private const int SettingFieldCount = 3;
        private const float VolumesHeaderButonWidth = 50f;
        private const float SliderOptionalLabelWidth = 50f;

        private SerializedProperty _applyOnEnableProp, _onlyApplyOnceProp, _resetOnDisableProp, _fadeTimeProp,
            _settingsProp, _sliderTypeProp, _allowBoostProp;

        private ReorderableList _settingsList = null;
        private RectOffset _inspectorPadding = null;
        private bool _isEditingInPlayMode = false;
        private BroInstructionHelper _instruction = new BroInstructionHelper();

        private readonly Rect[] _settingRects = new Rect[SettingFieldCount];
        private readonly float[] _settingRectRatio = new float[] { 0.325f, 0.325f, 0.35f };

        private GUIContent _volumeGUIContent, _readFromSliderGUIContent, _setToSliderGUIContent, _fadeTimeGUIContent,
            _editInPlaymodeGUIContent, _applyOnEnableGUIContent, _allowBoostGUIContent, _resetOnDisableGUIContent;

        private SliderType SliderType => (SliderType)_sliderTypeProp.enumValueIndex;
        private float MaxFieldWidth => EditorGUIUtility.labelWidth + 160f;

        private void OnEnable()
        {
            _applyOnEnableProp = serializedObject.FindProperty(NameOf.ApplyOnEnable);
            _onlyApplyOnceProp = serializedObject.FindProperty(NameOf.OnlyApplyOnce);
            _resetOnDisableProp = serializedObject.FindProperty(NameOf.ResetOnDisable);
            _fadeTimeProp = serializedObject.FindProperty(NameOf.FadeTime);
            _settingsProp = serializedObject.FindProperty(NameOf.Settings);
            _sliderTypeProp = serializedObject.FindProperty(NameOf.SliderType);
            _allowBoostProp = serializedObject.FindProperty(NameOf.AllowBoost);

            InitGUIContents();

            _inspectorPadding = InspectorPadding;

            _settingsList = new ReorderableList(serializedObject, _settingsProp)
            {
                drawHeaderCallback = OnDrawHeader,
                drawElementCallback = OnDrawElement,
                drawElementBackgroundCallback = OnDrawlelmentBackground,
                elementHeight = EditorGUIUtility.singleLineHeight * SettingFieldCount + ReorderableList.Defaults.padding,
                onAddCallback = OnAddElement,
            };
        }

        private void InitGUIContents()
        {
            _volumeGUIContent = new GUIContent("Volume");
            _readFromSliderGUIContent = new GUIContent("Read", "Read Volume From Slider");
            _setToSliderGUIContent = new GUIContent("Set", "Set Volume To Slider");
            _editInPlaymodeGUIContent = new GUIContent("Edit in Playmode", _instruction.GetText(Instruction.SoundVolume_EditInPlayMode));
            _applyOnEnableGUIContent = new GUIContent(_applyOnEnableProp.displayName, _instruction.GetText(Instruction.SoundVolume_ApplyOnEnable));
            _resetOnDisableGUIContent = new GUIContent(_resetOnDisableProp.displayName, _instruction.GetText(Instruction.SoundVolume_ResetOnDisable));
            _fadeTimeGUIContent = new GUIContent(_fadeTimeProp.displayName, _instruction.GetText(Instruction.SoundVolume_FadeTime));
            _allowBoostGUIContent = new GUIContent(_allowBoostProp.displayName, _instruction.GetText(Instruction.SoundVolume_AllowBoost));
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var property = _settingsProp.GetArrayElementAtIndex(index);
            var audioTypeProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.AudioType);
            var sliderProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.Slider);
            var volProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.Volume);

            rect.y += 2f;
            rect.height -= 4f;
            SplitRectVertical(rect, 2f, _settingRects, _settingRectRatio);

            EditorGUI.PropertyField(_settingRects[0], audioTypeProp);
            DrawSliderObjectField(_settingRects[1], sliderProp);
            

            Rect volRect = _settingRects[2];
            bool allowBoost = _allowBoostProp.boolValue && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL;
            float maxVolume = allowBoost ? AudioConstant.MaxVolume : AudioConstant.FullVolume;

            if (_isEditingInPlayMode)
            {
                EditorGUI.BeginChangeCheck();
            }
            volProp.floatValue = SliderType switch
            {
                SliderType.Linear => EditorGUI.Slider(volRect, _volumeGUIContent, volProp.floatValue, 0f, maxVolume),
                SliderType.Logarithmic => DrawLogarithmicSlider(volRect, _volumeGUIContent, volProp.floatValue, maxVolume),
                SliderType.BroVolume => BroEditorUtility.DrawVolumeSlider(volRect, _volumeGUIContent, volProp.floatValue, allowBoost),
                _ => throw new NotImplementedException(),
            };

            if (_isEditingInPlayMode && EditorGUI.EndChangeCheck())
            {
                SetVolumeToSlider(sliderProp, volProp.floatValue);
                BroAudio.SetVolume((BroAudioType)audioTypeProp.GetEnumFlag(), volProp.floatValue);
            }

            static float DrawLogarithmicSlider(Rect rect, GUIContent content, float vol, float maxVolume)
            {
                Rect suffixRect = EditorGUI.PrefixLabel(rect, content);
                return DrawLogarithmicSlider_Horizontal(suffixRect, vol, AudioConstant.MinVolume, maxVolume);
            }

            static void DrawSliderObjectField(Rect rect, SerializedProperty property)
            {
                Rect suffixRect = EditorGUI.PrefixLabel(rect, TempContent("Slider"));
                EditorGUI.ObjectField(suffixRect, property, GUIContent.none);

                if(property.objectReferenceValue == null && EditorGUIUtility.currentViewWidth > 300f)
                {
                    Rect noteRect = new Rect(suffixRect) { x = suffixRect.xMax - SliderOptionalLabelWidth - 20f, width = SliderOptionalLabelWidth };
                    EditorGUI.LabelField(noteRect, "Optional".SetColor(Color.grey).ToItalics(), GUIStyleHelper.RichText);
                }
            }
        }

        private void OnDrawlelmentBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(index < 0)
            {
                return;
            }

            var property = _settingsProp.GetArrayElementAtIndex(index);
            var audioTypeProp = property.FindPropertyRelative(SoundVolume.Setting.NameOf.AudioType);
            int flags = audioTypeProp.GetEnumFlag();
            bool isSelected = index == _settingsList.index;

            if(isSelected && Event.current.type == EventType.Repaint)
            {
                GUI.skin.window.Draw(rect, false, false, false, false);
                rect = EditorScriptingExtension.PolarCoordinates(rect, -1f);
                EditorGUI.DrawRect(rect, BroAudioGUISetting.SelectedColor);
                return;
            }
            
            if (flags >= 1)
            {
                int audioType = FlagsExtension.GetFirstFlag(flags);
                if (BroEditorUtility.EditorSetting.TryGetAudioTypeSetting((BroAudioType)audioType, out var setting))
                {
                    Color color = setting.Color;
                    color.a = 0.15f;
                    EditorGUI.DrawRect(rect, color);
                }
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

            if(_settingsList.count > 0 && !Application.isPlaying)
            {
                Rect buttonRect1 = new Rect(rect) { x = rect.xMax - VolumesHeaderButonWidth, width = VolumesHeaderButonWidth };
                Rect buttonRect2 = new Rect(rect) { x = buttonRect1.x - VolumesHeaderButonWidth - 5f, width = VolumesHeaderButonWidth };

                if (GUI.Button(buttonRect1, _setToSliderGUIContent))
                {
                    SetAllVolumeToSlider();
                }
                if (GUI.Button(buttonRect2, _readFromSliderGUIContent))
                {
                    ReadALlVolumeFromSlider();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            float drawedPosY = 1f;

            DrawBackgroudWindow(2, _inspectorPadding, ref drawedPosY);
            DrawBoldToggle(ref _applyOnEnableProp, _inspectorPadding, _applyOnEnableGUIContent);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledGroupScope(!_applyOnEnableProp.boolValue))
            {
                _onlyApplyOnceProp.boolValue = EditorGUILayout.Toggle(_onlyApplyOnceProp.displayName, _onlyApplyOnceProp.boolValue);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_resetOnDisableProp, _resetOnDisableGUIContent);
            EditorGUILayout.PropertyField(_fadeTimeProp, _fadeTimeGUIContent, GUILayout.Width(MaxFieldWidth));
            EditorGUILayout.Space();


            EditorGUILayout.LabelField("Slider", EditorStyles.boldLabel);
#if UNITY_WEBGL
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle(_allowBoostProp.displayName, false);
            EditorGUI.EndDisabledGroup();
#else
            EditorGUILayout.PropertyField(_allowBoostProp, _allowBoostGUIContent);
#endif
            DrawSliderTypeField();
            EditorGUILayout.Space();
            _settingsList.DoLayoutList();

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    DrawDebuggingButton();
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDebuggingButton()
        {
            _isEditingInPlayMode = GUILayout.Toggle(_isEditingInPlayMode, _editInPlaymodeGUIContent, EditorStyles.miniButton, GUILayout.Width(120f));
        }

        private void DrawSliderTypeField()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(_sliderTypeProp, GUILayout.Width(MaxFieldWidth));
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

        private void ReadALlVolumeFromSlider()
        {
            for(int i = 0; i < _settingsList.count; i++)
            {
                var settingProp = _settingsList.serializedProperty.GetArrayElementAtIndex(i);
                var volProp = settingProp.FindPropertyRelative(SoundVolume.Setting.NameOf.Volume);
                var sliderProp = settingProp.FindPropertyRelative(SoundVolume.Setting.NameOf.Slider);
                if (sliderProp.objectReferenceValue is UnityEngine.UI.Slider slider && slider)
                {
                    float vol = Utility.SliderToVolume(SliderType, slider.normalizedValue, _allowBoostProp.boolValue);
                    volProp.floatValue = (float)Math.Round(vol, RoundingDigits);
                }
            }
            _settingsList.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void SetAllVolumeToSlider()
        {
            for (int i = 0; i < _settingsList.count; i++)
            {
                var settingProp = _settingsList.serializedProperty.GetArrayElementAtIndex(i);
                var volProp = settingProp.FindPropertyRelative(SoundVolume.Setting.NameOf.Volume);
                var sliderProp = settingProp.FindPropertyRelative(SoundVolume.Setting.NameOf.Slider);
                SetVolumeToSlider(sliderProp, volProp.floatValue);
            }

            _settingsList.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void SetVolumeToSlider(SerializedProperty property, float vol)
        {
            if(property.objectReferenceValue is UnityEngine.UI.Slider slider && slider)
            {
                float normalizedValue = Utility.VolumeToSlider(SliderType, vol, _allowBoostProp.boolValue);
                slider.normalizedValue = normalizedValue;
            }
        }
    }
}