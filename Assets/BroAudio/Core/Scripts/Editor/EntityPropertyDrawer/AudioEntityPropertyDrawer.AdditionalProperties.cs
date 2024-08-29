using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.EditorSetting;
using static Ami.BroAudio.Data.AudioEntity;
using static Ami.BroAudio.Editor.BroEditorUtility;
using Ami.BroAudio.Runtime;
using System;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    [CustomPropertyDrawer(typeof(AudioEntity))]
    public partial class AudioEntityPropertyDrawer : MiPropertyDrawer
    {
        private const float Percentage = 100f;
        private const float RandomToolBarWidth = 40f;
        
        private readonly GUIContent _masterVolLabel = new GUIContent("Master Volume","Represent the master volume of all clips");
        private readonly GUIContent _loopingLabel = new GUIContent("Looping");
        private readonly GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
        private readonly GUIContent _pitchLabel = new GUIContent(nameof(AudioEntity.Pitch));
        private readonly GUIContent _spatialLabel = new GUIContent("Spatial (3D Sound)");
        private readonly float[] _seamlessSettingRectRatio = new float[] { 0.25f, 0.25f, 0.2f, 0.15f, 0.15f };

        private Rect[] _loopingRects = null;
        private Rect[] _seamlessRects = null;
        private SerializedProperty[] _loopingToggles = new SerializedProperty[2];

        private static int OverallDrawedPropStartIndex => DrawedPropertyConstant.OverallPropertyStartIndex;

        private float GetAdditionalBasePropertiesHeight(SerializedProperty property, AudioTypeSetting setting)
        {
            var drawFlags = setting.DrawedProperty;
            ConvertUnityEverythingFlagsToAll(ref drawFlags);
            int intBits = 32;
            int lineCount = GetDrawingLineCount(property, drawFlags, OverallDrawedPropStartIndex, intBits, IsDefaultValue);
            var seamlessProp = property.FindBackingFieldProperty(nameof(AudioEntity.SeamlessLoop));
            if (seamlessProp.boolValue)
            {
                lineCount++;
            }

            float offset = 0f;
            offset += IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.Priority) ? 0f : TwoSidesLabelOffsetY;
            offset += IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.MasterVolume) ? 0f : SnapVolumePadding;

            return lineCount * SingleLineSpace + offset;
        }

        private float GetAdditionalClipPropertiesHeight(SerializedProperty property, AudioTypeSetting setting)
        {
            var drawFlags = setting.DrawedProperty;
            ConvertUnityEverythingFlagsToAll(ref drawFlags);
            int lineCount = GetDrawingLineCount(property, drawFlags, 0, OverallDrawedPropStartIndex - 1, IsDefaultValue);
            return lineCount * SingleLineSpace;
        }

        private int GetDrawingLineCount(SerializedProperty property, DrawedProperty flags, int startIndex, int lastIndex, Func<SerializedProperty, DrawedProperty, bool> onGetIsDefaultValue)
        {
            int count = 0;
            for (int i = startIndex; i <= lastIndex; i++)
            {
                int drawFlag = (1 << i);
                if(drawFlag > (int)DrawedProperty.All)
                {
                    break;
                }
                if(!DrawedProperty.All.Contains((DrawedProperty)drawFlag))
                {
                    continue;
                }
                
                bool canDraw = ((int)flags & drawFlag) != 0;
                if (canDraw || !onGetIsDefaultValue.Invoke(property, (DrawedProperty)drawFlag))
                {
                    count++;
                }
            }
            return count;
        }

        private void DrawAdditionalBaseProperties(Rect position, SerializedProperty property, AudioTypeSetting setting)
        {
            var drawFlags = setting.DrawedProperty;
            ConvertUnityEverythingFlagsToAll(ref drawFlags);
            DrawMasterVolume();
            DrawPitchProperty();
            DrawPriorityProperty();
            DrawLoopProperty();
            DrawSpatialSetting();

            void DrawMasterVolume()
            {
                if (IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.MasterVolume, out var masterVolProp, out var volRandProp))
                {
                    return;
                }

                Offset += SnapVolumePadding;
                Rect masterVolRect = GetRectAndIterateLine(position);
                masterVolRect.width *= DefaultFieldRatio;
                
                SerializedProperty snapVolProp = property.FindPropertyRelative(EditorPropertyName.SnapToFullVolume);

                Rect randButtonRect = new Rect(masterVolRect.xMax + 5f, masterVolRect.y, RandomToolBarWidth, masterVolRect.height);
                if (DrawRandomButton(randButtonRect, RandomFlag.Volume, property))
                {
                    
                    float vol = masterVolProp.floatValue;
                    float volRange = volRandProp.floatValue;

                    Action<Rect> onDrawVU = null;
#if !UNITY_WEBGL
                    if(BroEditorUtility.EditorSetting.ShowVUColorOnVolumeSlider)
                    {
                        onDrawVU = sliderRect => DrawVUMeter(sliderRect, Setting.BroAudioGUISetting.VUMaskColor);
                    }
#endif
                    GetMixerMinMaxVolume(out float minVol, out float maxVol);
                    DrawRandomRangeSlider(masterVolRect,_masterVolLabel,ref vol,ref volRange, minVol, maxVol, SliderType.BroVolume, onDrawVU);
                    masterVolProp.floatValue = vol;
                    volRandProp.floatValue = volRange;
                }
                else
                {
                    masterVolProp.floatValue = DrawVolumeSlider(masterVolRect, _masterVolLabel, masterVolProp.floatValue, snapVolProp.boolValue, () =>
                    {
                        snapVolProp.boolValue = !snapVolProp.boolValue;
                    });
                }
            }

            void DrawLoopProperty()
            {
                if (IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.Loop, out var loopProp, out var seamlessProp))
                {
                    return;
                }

                _loopingToggles[0] = loopProp;
                _loopingToggles[1] = seamlessProp;

                Rect loopRect = GetRectAndIterateLine(position);
                _loopingRects ??= new Rect[_loopingToggles.Length];
                _loopingRects[0] = new Rect(loopRect) { width = 100f, x = loopRect.x + EditorGUIUtility.labelWidth };
                _loopingRects[1] = new Rect(loopRect) { width = 200f, x = _loopingRects[0].xMax };
                DrawToggleGroup(loopRect, _loopingLabel, _loopingToggles, _loopingRects);

                if (seamlessProp.boolValue)
                {
                    DrawSeamlessSetting(position, property);
                }
            }

            void DrawPitchProperty()
            {
                if (IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.Pitch, out var pitchProp, out var pitchRandProp))
                {
                    return;
                }
                
                Rect pitchRect = GetRectAndIterateLine(position);
                pitchRect.width *= DefaultFieldRatio;

                bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
                var pitchSetting = isWebGL? PitchShiftingSetting.AudioSource : BroEditorUtility.RuntimeSetting.PitchSetting;
                float minPitch = AudioConstant.MinPlayablePitch;
                float maxPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MaxMixerPitch : AudioConstant.MaxAudioSourcePitch;
                _pitchLabel.tooltip = $"According to the current preference setting, the Pitch will be set on [{pitchSetting}] ";

                Rect randButtonRect = new Rect(pitchRect.xMax + 5f, pitchRect.y, RandomToolBarWidth, pitchRect.height);
                bool hasRandom = DrawRandomButton(randButtonRect, RandomFlag.Pitch, property);

                float pitch = Mathf.Clamp(pitchProp.floatValue, minPitch, maxPitch);
                float pitchRange = pitchRandProp.floatValue;

                switch (pitchSetting)
                {
                    case PitchShiftingSetting.AudioMixer:
                        pitch = (float)Math.Round(pitch * Percentage, MidpointRounding.AwayFromZero);
                        pitchRange = (float)Math.Round(pitchRange * Percentage, MidpointRounding.AwayFromZero);
                        minPitch *= Percentage;
                        maxPitch *= Percentage;
                        if (hasRandom)
                        {
                            DrawRandomRangeSlider(pitchRect,_pitchLabel, ref pitch, ref pitchRange, minPitch, maxPitch, SliderType.Linear);
                            Rect minFieldRect = new Rect(pitchRect) { x = pitchRect.x + EditorGUIUtility.labelWidth + 5f, width = MinMaxSliderFieldWidth };
                            Rect maxFieldRect = new Rect(minFieldRect) { x = pitchRect.xMax - MinMaxSliderFieldWidth };
                            DrawPercentageLabel(minFieldRect);
                            DrawPercentageLabel(maxFieldRect);
                        }
                        else
                        {
                            pitch = EditorGUI.Slider(pitchRect, _pitchLabel, pitch, minPitch, maxPitch);
                            DrawPercentageLabel(pitchRect);
                        }
                        pitch /= Percentage;
                        pitchRange /= Percentage;
                        break;

                    case PitchShiftingSetting.AudioSource:
                        if (hasRandom)
                        {
                            DrawRandomRangeSlider(pitchRect, _pitchLabel,ref pitch, ref pitchRange, minPitch, maxPitch, SliderType.Linear);
                        }
                        else
                        {
                            pitch = EditorGUI.Slider(pitchRect, _pitchLabel, pitch, minPitch, maxPitch);
                        }
                        break;
                }

                pitchProp.floatValue = pitch;
                pitchRandProp.floatValue = pitchRange;
            }

            void DrawPriorityProperty()
            {
                if (IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.Priority, out var priorityProp, out _))
                {
                    return;
                }

                Rect priorityRect = GetRectAndIterateLine(position);
                priorityRect.width *= DefaultFieldRatio;

                MultiLabel multiLabels = new MultiLabel() { Main = nameof(AudioEntity.Priority), Left = "High", Right = "Low" };
                priorityProp.intValue = (int)Draw2SidesLabelSlider(priorityRect, multiLabels, priorityProp.intValue, AudioConstant.HighestPriority, AudioConstant.LowestPriority);
                Offset += TwoSidesLabelOffsetY;
            }

            void DrawSpatialSetting()
            {
                if (IsDefaultValueAndCanNotDraw(property, drawFlags, DrawedProperty.SpatialSettings, out var spatialProp, out _))
                {
                    return;
                }

                Rect suffixRect = EditorGUI.PrefixLabel(GetRectAndIterateLine(position), _spatialLabel);
                SplitRectHorizontal(suffixRect, 0.5f, 5f, out Rect objFieldRect, out Rect buttonRect);
                EditorGUI.ObjectField(objFieldRect, spatialProp, GUIContent.none);
                bool hasSetting = spatialProp.objectReferenceValue != null;
                string buttonLabel = hasSetting ? "Open Panel" : "Create And Open";
                if (GUI.Button(buttonRect, buttonLabel))
                {
                    if (!hasSetting)
                    {
                        string entityName = property.FindPropertyRelative(GetBackingFieldName(nameof(IEntityIdentity.Name))).stringValue;
                        string path = EditorUtility.SaveFilePanelInProject("Save Spatial Setting", entityName + "_Spatial", "asset", "Message");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var newSetting = ScriptableObject.CreateInstance<SpatialSetting>();
                            AssetDatabase.CreateAsset(newSetting, path);
                            spatialProp.objectReferenceValue = newSetting;
                            spatialProp.serializedObject.ApplyModifiedProperties();

                            SpatialSettingsEditorWindow.ShowWindow(spatialProp);
                            GUIUtility.ExitGUI();
                            AssetDatabase.SaveAssets();
                        }
                    }
                    else
                    {
                        SpatialSettingsEditorWindow.ShowWindow(spatialProp);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private bool IsDefaultValue(SerializedProperty property, DrawedProperty drawedProperty)
        {
            return IsDefaultValue(property, drawedProperty, out _, out _);
        }


        private bool IsDefaultValue(SerializedProperty property, DrawedProperty drawedProperty, out SerializedProperty mainProp, out SerializedProperty secondaryProp)
        {
            mainProp = null;
            secondaryProp = null;
            switch (drawedProperty)
            {
                case DrawedProperty.MasterVolume:
                    mainProp = property.FindBackingFieldProperty(nameof(AudioEntity.MasterVolume));
                    secondaryProp = property.FindBackingFieldProperty(nameof(AudioEntity.VolumeRandomRange));
                    return mainProp.floatValue == AudioConstant.FullVolume && secondaryProp.floatValue == 0f;
                case DrawedProperty.Loop:
                    mainProp = property.FindBackingFieldProperty(nameof(AudioEntity.Loop));
                    secondaryProp = property.FindBackingFieldProperty(nameof(AudioEntity.SeamlessLoop));
                    return !mainProp.boolValue && !secondaryProp.boolValue;
                case DrawedProperty.Priority:
                    mainProp = property.FindBackingFieldProperty(nameof(AudioEntity.Priority));
                    return mainProp.intValue == AudioConstant.DefaultPriority;
                case DrawedProperty.SpatialSettings:
                    mainProp = property.FindBackingFieldProperty(nameof(AudioEntity.SpatialSetting));
                    return mainProp.objectReferenceValue == null;
                case DrawedProperty.Pitch:
                    mainProp = property.FindBackingFieldProperty(nameof(AudioEntity.Pitch));
                    secondaryProp = property.FindBackingFieldProperty(nameof(AudioEntity.PitchRandomRange));
                    return mainProp.floatValue == AudioConstant.DefaultPitch && secondaryProp.floatValue == 0f;
            }
            return true;
        }

        private bool IsDefaultValueAndCanNotDraw(SerializedProperty checkedProp, DrawedProperty drawFlags, DrawedProperty drawTarget)
        {
            return IsDefaultValueAndCanNotDraw(checkedProp, drawFlags, drawTarget, out _, out _);
        }

        private bool IsDefaultValueAndCanNotDraw(SerializedProperty checkedProp, DrawedProperty drawFlags, DrawedProperty drawTarget, out SerializedProperty mainProp, out SerializedProperty secondaryProp)
        {
            return IsDefaultValue(checkedProp, drawTarget, out mainProp, out secondaryProp) && !drawFlags.Contains(drawTarget);
        }

        private void DrawPercentageLabel(Rect fieldRect)
        {
            float width = 15f;
            Rect percentageRect = new Rect(fieldRect) { width = width, x = fieldRect.xMax - width };
            EditorGUI.LabelField(percentageRect, "%");
        }

        private void DrawAdditionalClipProperties(Rect position, SerializedProperty property, AudioTypeSetting setting)
        {
        }

        private void DrawSeamlessSetting(Rect totalPosition, SerializedProperty property)
        {
            Rect suffixRect = EditorGUI.PrefixLabel(GetRectAndIterateLine(totalPosition), _seamlessLabel);
            _seamlessRects ??= new Rect[_seamlessSettingRectRatio.Length];
            SplitRectHorizontal(suffixRect, 10f, _seamlessRects, _seamlessSettingRectRatio);

            int drawIndex = 0;
            EditorGUI.LabelField(_seamlessRects[drawIndex], "Transition By");
            drawIndex++;

            var seamlessTypeProp = property.FindPropertyRelative(EditorPropertyName.SeamlessType);
            SeamlessType currentType = (SeamlessType)seamlessTypeProp.enumValueIndex;
            currentType = (SeamlessType)EditorGUI.EnumPopup(_seamlessRects[drawIndex], currentType);
            seamlessTypeProp.enumValueIndex = (int)currentType;
            drawIndex++;

            var transitionTimeProp = property.FindBackingFieldProperty(nameof(AudioEntity.TransitionTime));
            switch (currentType)
            {
                case SeamlessType.Time:
                    transitionTimeProp.floatValue = Mathf.Abs(EditorGUI.FloatField(_seamlessRects[drawIndex], transitionTimeProp.floatValue));
                    break;
                case SeamlessType.Tempo:
                    var tempoProp = property.FindPropertyRelative(EditorPropertyName.TransitionTempo);
                    var bpmProp = tempoProp.FindPropertyRelative(nameof(TempoTransition.BPM));
                    var beatsProp = tempoProp.FindPropertyRelative(nameof(TempoTransition.Beats));

                    SplitRectHorizontal(_seamlessRects[drawIndex], 0.5f, 2f, out var tempoValue, out var tempoLabel);
                    bpmProp.floatValue = Mathf.Abs(EditorGUI.FloatField(tempoValue, bpmProp.floatValue));
                    EditorGUI.LabelField(tempoLabel, "BPM");
                    drawIndex++;

                    SplitRectHorizontal(_seamlessRects[drawIndex], 0.5f, 2f, out var beatsValue, out var beatsLabel);
                    beatsProp.intValue = EditorGUI.IntField(beatsValue, beatsProp.intValue);
                    EditorGUI.LabelField(beatsLabel, "Beats");

                    transitionTimeProp.floatValue = Mathf.Abs(AudioExtension.TempoToTime(bpmProp.floatValue, beatsProp.intValue));
                    break;
                case SeamlessType.ClipSetting:
                    transitionTimeProp.floatValue = AudioPlayer.UseEntitySetting;
                    break;
            }
        }

        private bool DrawRandomButton(Rect rect,RandomFlag targetFlag, SerializedProperty property)
        {
            SerializedProperty randFlagsProp = property.FindBackingFieldProperty(nameof(AudioEntity.RandomFlags));
            RandomFlag randomFlags = (RandomFlag)randFlagsProp.intValue;
            bool hasRandom = randomFlags.Contains(targetFlag);
            hasRandom = GUI.Toggle(rect, hasRandom, "RND", EditorStyles.miniButton);
            randomFlags = hasRandom ? randomFlags | targetFlag : randomFlags & ~targetFlag;
            randFlagsProp.intValue = (int)randomFlags;
            return hasRandom;
        }

        private void ConvertUnityEverythingFlagsToAll(ref DrawedProperty drawedProperty)
        {
            if((int)drawedProperty == -1)
            {
                drawedProperty = DrawedProperty.All;
            }
        }
    }
}
