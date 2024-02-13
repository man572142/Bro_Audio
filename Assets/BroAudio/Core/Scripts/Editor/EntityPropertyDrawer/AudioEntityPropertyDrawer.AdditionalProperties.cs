using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.Extension.FlagsExtension;
using static Ami.BroAudio.Editor.EditorSetting;
using static Ami.BroAudio.Data.AudioEntity;
using static Ami.BroAudio.Editor.BroEditorUtility;
using Ami.BroAudio.Runtime;
using System;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioEntity))]
	public partial class AudioEntityPropertyDrawer : MiPropertyDrawer
	{
		private const float Percentage = 100f;
		private const float RandomToolBarWidth = 40f;
		private const float MinMaxSliderFieldWidth = 50f;
		private const int RoundedDigits = 3;

		private readonly GUIContent _masterVolLabel = new GUIContent("Master Volume","Represent the master volume of all clips");
		private readonly GUIContent _loopingLabel = new GUIContent("Looping");
		private readonly GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
		private readonly GUIContent _pitchLabel = new GUIContent(nameof(AudioEntity.Pitch));
		private readonly GUIContent _spatialLabel = new GUIContent("Spatial (3D Sound)");
		private readonly float[] _seamlessSettingRectRatio = new float[] { 0.2f, 0.25f, 0.2f, 0.2f, 0.15f };

		private Rect[] _seamlessRects = null;
		private SerializedProperty[] _loopingToggles = new SerializedProperty[2];

		private int GetAdditionalBaseProtiesLineCount(SerializedProperty property, AudioTypeSetting setting)
		{
			int filterRange = GetFlagsRange(0, DrawedPropertyConstant.AdditionalPropertyStartIndex -1 ,FlagsRangeType.Excluded);
			ConvertUnityEverythingFlagsToAll(ref setting.DrawedProperty);
			int count = GetFlagsOnCount((int)setting.DrawedProperty & filterRange);

			var seamlessProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.SeamlessLoop));
            if (seamlessProp.boolValue)
			{
				count++;
			}

			return count; 
		}

		private float GetAdditionalBasePropertiesOffest(AudioTypeSetting setting)
		{
			float offset = 0f;
			offset += setting.DrawedProperty.Contains(DrawedProperty.Priority) ? TwoSidesLabelOffsetY : 0f;
			offset += setting.DrawedProperty.Contains(DrawedProperty.MasterVolume) ? SnapVolumePadding : 0f;
			return offset;
		}

		private int GetAdditionalClipPropertiesLineCount(SerializedProperty property, AudioTypeSetting setting)
		{
            int filterRange = GetFlagsRange(0, DrawedPropertyConstant.AdditionalPropertyStartIndex - 1, FlagsRangeType.Included);
			ConvertUnityEverythingFlagsToAll(ref setting.DrawedProperty);
			return GetFlagsOnCount((int)setting.DrawedProperty & filterRange);
        }

		private void DrawAdditionalBaseProperties(Rect position, SerializedProperty property, AudioTypeSetting setting)
		{
			DrawMasterVolume();
			DrawPitchProperty();
			DrawPriorityProperty();
			DrawLoopProperty();
			DrawSpatialSettings();

			void DrawMasterVolume()
			{
				if (!setting.DrawedProperty.Contains(DrawedProperty.MasterVolume))
				{
					return;
				}

				Offset += SnapVolumePadding;
				Rect masterVolRect = GetRectAndIterateLine(position);
				masterVolRect.width *= DefaultFieldRatio;
				SerializedProperty masterVolProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.MasterVolume));
				SerializedProperty snapVolProp = property.FindPropertyRelative(EditorPropertyName.SnapToFullVolume);

				Rect randButtonRect = new Rect(masterVolRect.xMax + 5f, masterVolRect.y, RandomToolBarWidth, masterVolRect.height);
				if (DrawRandomButton(randButtonRect, RandomFlags.Volume, property))
				{
					SerializedProperty volRandProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.VolumeRandomRange));
					float vol = masterVolProp.floatValue;
					float volRange = volRandProp.floatValue;

					Action<Rect> onDrawVU = null;
#if !UNITY_WEBGL
					if(BroEditorUtility.EditorSetting.ShowVUColorOnVolumeSlider)
					{
						onDrawVU = sliderRect => DrawVUMeter(sliderRect, Setting.BroAudioGUISetting.VUMaskColor);
					}
#endif
					bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
					DrawRandomRangeSlider(masterVolRect,_masterVolLabel,ref vol,ref volRange, AudioConstant.MinVolume, isWebGL? AudioConstant.FullVolume : AudioConstant.MaxVolume,RandomRangeSliderType.BroVolume, onDrawVU);
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
				if (setting.DrawedProperty.Contains(DrawedProperty.Loop))
				{
					_loopingToggles[0] = GetBackingNameAndFindProperty(property,nameof(AudioEntity.Loop));
					_loopingToggles[1] = GetBackingNameAndFindProperty(property,nameof(AudioEntity.SeamlessLoop));

					Rect loopRect = GetRectAndIterateLine(position);
					DrawToggleGroup(loopRect, _loopingLabel, _loopingToggles);

					if (_loopingToggles[1].boolValue)
					{
						DrawSeamlessSetting(position, property);
					}
				}
			}

			void DrawPitchProperty()
			{
				if (!setting.DrawedProperty.Contains(DrawedProperty.Pitch))
				{
					return;
				}
				
				Rect pitchRect = GetRectAndIterateLine(position);
				pitchRect.width *= DefaultFieldRatio;

				bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
				var pitchSetting = isWebGL? PitchShiftingSetting.AudioSource : BroEditorUtility.RuntimeSetting.PitchSetting;
				float minPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MinMixerPitch : AudioConstant.MinAudioSourcePitch;
				float maxPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MaxMixerPitch : AudioConstant.MaxAudioSourcePitch;
				_pitchLabel.tooltip = $"Pitch will be set on [{pitchSetting}] according to the current global setting";

				SerializedProperty pitchProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.Pitch));
				SerializedProperty pitchRandProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.PitchRandomRange));

				Rect randButtonRect = new Rect(pitchRect.xMax + 5f, pitchRect.y, RandomToolBarWidth, pitchRect.height);
				bool hasRandom = DrawRandomButton(randButtonRect, RandomFlags.Pitch, property);

				float pitch = Mathf.Clamp(pitchProp.floatValue, minPitch, maxPitch);
				float pitchRange = pitchRandProp.floatValue;

				switch (pitchSetting)
				{
					case PitchShiftingSetting.AudioMixer:
						pitch = Mathf.Round(pitch * Percentage);
						pitchRange = Mathf.Round(pitchRange * Percentage);
						minPitch *= Percentage;
						maxPitch *= Percentage;
						if (hasRandom)
						{
							DrawRandomRangeSlider(pitchRect,_pitchLabel, ref pitch, ref pitchRange, minPitch, maxPitch,RandomRangeSliderType.Default);
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
							DrawRandomRangeSlider(pitchRect, _pitchLabel,ref pitch, ref pitchRange, minPitch, maxPitch, RandomRangeSliderType.Default);
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
				if (setting.DrawedProperty.Contains(DrawedProperty.Priority))
				{
					Rect priorityRect = GetRectAndIterateLine(position);
					priorityRect.width *= DefaultFieldRatio;
					SerializedProperty priorityProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.Priority));

					MultiLabel multiLabels = new MultiLabel() { Main = nameof(AudioEntity.Priority), Left = "High", Right = "Low"};
					priorityProp.intValue = (int)Draw2SidesLabelSlider(priorityRect, multiLabels, priorityProp.intValue, AudioConstant.HighestPriority, AudioConstant.LowestPriority);
					Offset += TwoSidesLabelOffsetY;
				}
			}

			void DrawSpatialSettings()
			{
				if (setting.DrawedProperty.Contains(DrawedProperty.SpatialSettings))
				{
					Rect suffixRect = EditorGUI.PrefixLabel(GetRectAndIterateLine(position), _spatialLabel);
					if(GUI.Button(suffixRect, "Open Setting Window"))
					{
						SerializedProperty settingsProp = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SpatialSettings)));
						SpatialSettingsEditorWindow.ShowWindow(settingsProp, OnSetSpatialSettingBack);
						GUIUtility.ExitGUI();
					}
				}
			}

			void OnSetSpatialSettingBack(SpatialSettings spatialSettings)
			{
				SerializedProperty prop = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SpatialSettings)));
				GetSpatialSettingsProperty(prop, SpatialPropertyType.StereoPan).floatValue = spatialSettings.StereoPan;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.DopplerLevel).floatValue = spatialSettings.DopplerLevel;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.MinDistance).floatValue = spatialSettings.MinDistance;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.MaxDistance).floatValue = spatialSettings.MaxDistance;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.SpatialBlend).animationCurveValue = spatialSettings.SpatialBlend;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.ReverbZoneMix).animationCurveValue = spatialSettings.ReverbZoneMix;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.Spread).animationCurveValue = spatialSettings.Spread;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.CustomRolloff).animationCurveValue = spatialSettings.CustomRolloff;
				GetSpatialSettingsProperty(prop, SpatialPropertyType.RolloffMode).enumValueIndex = (int)spatialSettings.RolloffMode;
				prop.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}
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
			_seamlessRects = _seamlessRects ?? new Rect[_seamlessSettingRectRatio.Length];
			SplitRectHorizontal(suffixRect, 10f, _seamlessRects, _seamlessSettingRectRatio);

            int drawIndex = 0;
			EditorGUI.LabelField(_seamlessRects[drawIndex], "Transition By");
			drawIndex++;

			var seamlessTypeProp = property.FindPropertyRelative(EditorPropertyName.SeamlessType);
			SeamlessType currentType = (SeamlessType)seamlessTypeProp.enumValueIndex;
			currentType = (SeamlessType)EditorGUI.EnumPopup(_seamlessRects[drawIndex], currentType);
			seamlessTypeProp.enumValueIndex = (int)currentType;
			drawIndex++;

			var transitionTimeProp = GetBackingNameAndFindProperty(property,nameof(AudioEntity.TransitionTime));
			switch (currentType)
			{
				// TODO : 數值不能超過Clip長度
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

		private void DrawRandomRangeSlider(Rect rect,GUIContent label,ref float value, ref float valueRange, float minLimit, float maxLimit,RandomRangeSliderType sliderType,Action<Rect> onGetSliderRect = null)
		{
			float minRand = value - valueRange * 0.5f;
			float maxRand = value + valueRange * 0.5f;
			minRand = (float)Math.Round(Mathf.Clamp(minRand, minLimit, maxLimit), RoundedDigits);
			maxRand = (float)Math.Round(Mathf.Clamp(maxRand, minLimit, maxLimit), RoundedDigits);
			switch (sliderType)
			{
				case RandomRangeSliderType.Default:
					DrawMinMaxSlider(rect, label, ref minRand, ref maxRand, minLimit, maxLimit, MinMaxSliderFieldWidth, onGetSliderRect);
					break;
				case RandomRangeSliderType.Logarithmic:
					DrawLogarithmicMinMaxSlider(rect, label, ref minRand, ref maxRand, minLimit, maxLimit, MinMaxSliderFieldWidth, onGetSliderRect);
					break;
				case RandomRangeSliderType.BroVolume:
					DrawRandomRangeVolumeSlider(rect, label, ref minRand, ref maxRand, minLimit, maxLimit, MinMaxSliderFieldWidth, onGetSliderRect);
					break;
			}			
			valueRange = maxRand - minRand;
			value = minRand + valueRange * 0.5f;
		}

		private bool DrawRandomButton(Rect rect,RandomFlags targetFlag, SerializedProperty property)
		{
			SerializedProperty randFlagsProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.RandomFlags));
			RandomFlags randomFlags = (RandomFlags)randFlagsProp.intValue;
			bool hasRandom = randomFlags.Contains(targetFlag);
			hasRandom = GUI.Toggle(rect, hasRandom, "RND", EditorStyles.miniButton);
			randomFlags = hasRandom ? randomFlags | targetFlag : randomFlags & ~targetFlag;
			randFlagsProp.intValue = (int)randomFlags;
			return hasRandom;
		}

		private SerializedProperty GetBackingNameAndFindProperty(SerializedProperty entityProp, string memberName)
		{
			return entityProp.FindPropertyRelative(GetBackingFieldName(memberName));
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
