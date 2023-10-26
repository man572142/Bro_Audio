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
using Ami.BroAudio.Runtime;
using System;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioEntity))]
	public partial class AudioEntityPropertyDrawer : MiPropertyDrawer
	{
		private GUIContent _loopingLabel = new GUIContent("Looping");
		private GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
		private SerializedProperty[] _loopingToggles = new SerializedProperty[2];

		private float[] _seamlessSettingRectRatio = new float[] { 0.2f, 0.25f, 0.2f, 0.2f, 0.15f };

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
			if(setting.DrawedProperty.HasFlag(DrawedProperty.Priority))
			{
				offset += TwoSidesLabelOffsetY;
			}
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
			DrawLoopProperty();
			DrawPitchProperty();
			DrawPriorityProperty();

			void DrawLoopProperty()
			{
				if (setting.DrawedProperty.HasFlag(DrawedProperty.Loop))
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
				if (!setting.DrawedProperty.HasFlag(DrawedProperty.Pitch))
				{
					return;
				}
				
				Rect pitchRect = GetRectAndIterateLine(position);
				pitchRect.width *= _defaultFieldRatio;
				SerializedProperty pitchProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.Pitch));

				bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
				var pitchSetting = isWebGL? PitchShiftingSetting.AudioSource : BroEditorUtility.RuntimeSetting.PitchSetting;
				float minPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MinMixerPitch : AudioConstant.MinAudioSourcePitch;
				float maxPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MaxMixerPitch : AudioConstant.MaxAudioSourcePitch;
				GUIContent label = new GUIContent(nameof(AudioEntity.Pitch), $"Pitch will be set on [{pitchSetting}] according to the current global setting");

				float pitch = Mathf.Clamp(pitchProp.floatValue, minPitch, maxPitch);
				switch (pitchSetting)
				{
					case PitchShiftingSetting.AudioMixer:
						float percentage = 100f;
						float displayValue = EditorGUI.Slider(pitchRect, label, Mathf.Round(pitch * percentage), minPitch * percentage, maxPitch * percentage);
						pitchProp.floatValue = displayValue / percentage;
						Rect percentageRect = new Rect(pitchRect);
						percentageRect.width = 15f;
						percentageRect.x = pitchRect.xMax - percentageRect.width;
						EditorGUI.LabelField(percentageRect, "%");
						break;
					case PitchShiftingSetting.AudioSource:
						pitchProp.floatValue = EditorGUI.Slider(pitchRect, label, pitch, minPitch, maxPitch);
						break;
				}
			}

			void DrawPriorityProperty()
			{
				if (setting.DrawedProperty.HasFlag(DrawedProperty.Priority))
				{
					Rect priorityRect = GetRectAndIterateLine(position);
					priorityRect.width *= _defaultFieldRatio;
					SerializedProperty priorityProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.Priority));

					MultiLabel multiLabels = new MultiLabel() { Main = nameof(AudioEntity.Priority), Left = "High", Right = "Low"};
					priorityProp.intValue = (int)Draw2SidesLabelSlider(priorityRect, multiLabels, priorityProp.intValue, AudioConstant.MinPriority, AudioConstant.MaxPriority);
					Offset += TwoSidesLabelOffsetY;
				}
			}
		}

		private void DrawAdditionalClipProperties(Rect position, SerializedProperty property, AudioTypeSetting setting)
		{

		}

		private void DrawSeamlessSetting(Rect totalPosition, SerializedProperty property)
		{
			Rect suffixRect = EditorGUI.PrefixLabel(GetRectAndIterateLine(totalPosition), _seamlessLabel);
			SplitRectHorizontal(suffixRect, 10f, out Rect[] rects, _seamlessSettingRectRatio);

            int drawIndex = 0;
			EditorGUI.LabelField(rects[drawIndex], "Transition By");
			drawIndex++;

			var seamlessTypeProp = property.FindPropertyRelative(NameOf.SeamlessType);
			SeamlessType currentType = (SeamlessType)seamlessTypeProp.enumValueIndex;
			currentType = (SeamlessType)EditorGUI.EnumPopup(rects[drawIndex], currentType);
			seamlessTypeProp.enumValueIndex = (int)currentType;
			drawIndex++;

			var transitionTimeProp = GetBackingNameAndFindProperty(property,nameof(AudioEntity.TransitionTime));
			switch (currentType)
			{
				// TODO : 數值不能超過Clip長度
				case SeamlessType.Time:
					transitionTimeProp.floatValue = Mathf.Abs(EditorGUI.FloatField(rects[drawIndex], transitionTimeProp.floatValue));
					break;
				case SeamlessType.Tempo:
					var tempoProp = property.FindPropertyRelative(NameOf.TransitionTempo);
					var bpmProp = tempoProp.FindPropertyRelative(nameof(TempoTransition.BPM));
					var beatsProp = tempoProp.FindPropertyRelative(nameof(TempoTransition.Beats));

					SplitRectHorizontal(rects[drawIndex], 0.5f, 2f, out var tempoValue, out var tempoLabel);
					bpmProp.floatValue = Mathf.Abs(EditorGUI.FloatField(tempoValue, bpmProp.floatValue));
					EditorGUI.LabelField(tempoLabel, "BPM");
					drawIndex++;

					SplitRectHorizontal(rects[drawIndex], 0.5f, 2f, out var beatsValue, out var beatsLabel);
					beatsProp.intValue = EditorGUI.IntField(beatsValue, beatsProp.intValue);
					EditorGUI.LabelField(beatsLabel, "Beats");

					transitionTimeProp.floatValue = Mathf.Abs(AudioExtension.TempoToTime(bpmProp.floatValue, beatsProp.intValue));
					break;
				case SeamlessType.ClipSetting:
					transitionTimeProp.floatValue = AudioPlayer.UseEntitySetting;
					break;
			}
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
