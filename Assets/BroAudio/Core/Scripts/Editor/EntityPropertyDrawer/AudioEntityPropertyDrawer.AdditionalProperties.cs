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
		private const int EntityPropertyDepth = 2; // e.g AudioEntity.MasterVolume

		private readonly GUIContent _masterVolLabel = new GUIContent("Master Volume","Represent the master volume of all clips");
		private readonly GUIContent _loopingLabel = new GUIContent("Looping");
		private readonly GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
		private readonly GUIContent _pitchLabel = new GUIContent(nameof(AudioEntity.Pitch));
		private readonly GUIContent _spatialLabel = new GUIContent("Spatial (3D Sound)");
		private readonly float[] _seamlessSettingRectRatio = new float[] { 0.2f, 0.25f, 0.2f, 0.2f, 0.15f };

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
                SerializedProperty masterVolProp = property.FindBackingFieldProperty(nameof(AudioEntity.MasterVolume));
				SerializedProperty volRandProp = property.FindBackingFieldProperty(nameof(AudioEntity.VolumeRandomRange));
				if (IsDefaultValueAndCanNotDraw(masterVolProp, drawFlags, DrawedProperty.MasterVolume) && volRandProp.floatValue == 0f)
				{
					return;
				}

				Offset += SnapVolumePadding;
				Rect masterVolRect = GetRectAndIterateLine(position);
				masterVolRect.width *= DefaultFieldRatio;
				
				SerializedProperty snapVolProp = property.FindPropertyRelative(EditorPropertyName.SnapToFullVolume);

				Rect randButtonRect = new Rect(masterVolRect.xMax + 5f, masterVolRect.y, RandomToolBarWidth, masterVolRect.height);
				if (DrawRandomButton(randButtonRect, RandomFlags.Volume, property))
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
                _loopingToggles[0] = property.FindBackingFieldProperty(nameof(AudioEntity.Loop));
                _loopingToggles[1] = property.FindBackingFieldProperty(nameof(AudioEntity.SeamlessLoop));

                if (IsDefaultValueAndCanNotDraw(_loopingToggles[0], drawFlags, DrawedProperty.Loop)
					&& IsDefaultValueAndCanNotDraw(_loopingToggles[1], drawFlags, DrawedProperty.Loop))
				{
					return;
				}

                Rect loopRect = GetRectAndIterateLine(position);
                DrawToggleGroup(loopRect, _loopingLabel, _loopingToggles);

                if (_loopingToggles[1].boolValue)
                {
                    DrawSeamlessSetting(position, property);
                }
            }

			void DrawPitchProperty()
			{
                SerializedProperty pitchProp = property.FindBackingFieldProperty(nameof(AudioEntity.Pitch));
                SerializedProperty pitchRandProp = property.FindBackingFieldProperty(nameof(AudioEntity.PitchRandomRange));
                if (IsDefaultValueAndCanNotDraw(pitchProp, drawFlags, DrawedProperty.Pitch) && pitchRandProp.floatValue == 0f)
				{
					return;
				}
				
				Rect pitchRect = GetRectAndIterateLine(position);
				pitchRect.width *= DefaultFieldRatio;

				bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
				var pitchSetting = isWebGL? PitchShiftingSetting.AudioSource : BroEditorUtility.RuntimeSetting.PitchSetting;
				float minPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MinMixerPitch : AudioConstant.MinAudioSourcePitch;
				float maxPitch = pitchSetting == PitchShiftingSetting.AudioMixer ? AudioConstant.MaxMixerPitch : AudioConstant.MaxAudioSourcePitch;
				_pitchLabel.tooltip = $"According to the current preference setting, the Pitch will be set on [{pitchSetting}] ";

				Rect randButtonRect = new Rect(pitchRect.xMax + 5f, pitchRect.y, RandomToolBarWidth, pitchRect.height);
				bool hasRandom = DrawRandomButton(randButtonRect, RandomFlags.Pitch, property);

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
                SerializedProperty priorityProp = property.FindBackingFieldProperty(nameof(AudioEntity.Priority));
                if (IsDefaultValueAndCanNotDraw(priorityProp, drawFlags, DrawedProperty.Priority))
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
                SerializedProperty spatialProp = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SpatialSetting)));
                if (IsDefaultValueAndCanNotDraw(spatialProp, drawFlags, DrawedProperty.SpatialSettings))
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
			switch (drawedProperty)
			{
				case DrawedProperty.MasterVolume:
					var masterVolProp = DigPropertyIfNeeded(nameof(AudioEntity.MasterVolume));
					return masterVolProp.floatValue == AudioConstant.FullVolume;
				case DrawedProperty.Loop:
					property = DigPropertyIfNeeded(nameof(AudioEntity.Loop));
					return property.boolValue == false;
				case DrawedProperty.Priority:
					property = DigPropertyIfNeeded(nameof(AudioEntity.Priority));
					return property.intValue == AudioConstant.DefaultPriority;
				case DrawedProperty.SpatialSettings:
					property = DigPropertyIfNeeded(nameof(AudioEntity.SpatialSetting));
					return property.objectReferenceValue == null;
				case DrawedProperty.Pitch:
					var pitchProp = DigPropertyIfNeeded(nameof(AudioEntity.Pitch));
					return pitchProp.floatValue == AudioConstant.DefaultPitch;
			}
			return true;

			SerializedProperty DigPropertyIfNeeded(string path, bool isBackingField = true)
			{
				if (property.depth < EntityPropertyDepth)
				{
					return property.FindPropertyRelative(isBackingField ? GetBackingFieldName(path) : path);
				}
				return property;
			}
		}

		private bool IsDefaultValueAndCanNotDraw(SerializedProperty checkedProp, DrawedProperty drawFlags, DrawedProperty drawTarget)
		{
			return IsDefaultValue(checkedProp, drawTarget) && !drawFlags.Contains(drawTarget);
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

		private void DrawRandomRangeSlider(Rect rect,GUIContent label,ref float value, ref float valueRange, float minLimit, float maxLimit,RandomRangeSliderType sliderType,Action<Rect> onGetSliderRect = null)
		{
			float minRand = value - valueRange * 0.5f;
			float maxRand = value + valueRange * 0.5f;
			minRand = (float)Math.Round(Mathf.Clamp(minRand, minLimit, maxLimit), RoundedDigits, MidpointRounding.AwayFromZero);
			maxRand = (float)Math.Round(Mathf.Clamp(maxRand, minLimit, maxLimit), RoundedDigits, MidpointRounding.AwayFromZero);
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
			SerializedProperty randFlagsProp = property.FindBackingFieldProperty(nameof(AudioEntity.RandomFlags));
			RandomFlags randomFlags = (RandomFlags)randFlagsProp.intValue;
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
