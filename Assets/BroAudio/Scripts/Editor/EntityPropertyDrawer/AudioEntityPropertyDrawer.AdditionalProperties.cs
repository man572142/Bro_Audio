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
			int count = GetFlagsOnCount((int)setting.DrawedProperty & filterRange);

			var seamlessProp = GetBackingNameAndFindProperty(property, nameof(AudioEntity.SeamlessLoop));
            if (seamlessProp.boolValue)
			{
				count++;
			}

			return count; 
		}

		private int GetAdditionalClipPropertiesLineCount(SerializedProperty property, AudioTypeSetting setting)
		{
            int filterRange = GetFlagsRange(0, DrawedPropertyConstant.AdditionalPropertyStartIndex - 1, FlagsRangeType.Included);
            return GetFlagsOnCount((int)setting.DrawedProperty & filterRange);
        }

		private void DrawAdditionalBaseProperties(Rect position, SerializedProperty property, AudioTypeSetting setting)
		{
			DrawDelayProperty();
			DrawLoopProperty();

			void DrawDelayProperty()
			{
				if (setting.DrawedProperty.HasFlag(DrawedProperty.Delay))
				{
					SerializedProperty delayProperty = GetBackingNameAndFindProperty(property,nameof(AudioEntity.Delay));
					delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
				}
			}

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
	} 
}
