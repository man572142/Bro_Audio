using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Data.AudioLibrary;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioLibrary))]
	public partial class AudioLibraryPropertyDrawer : MiPropertyDrawer
	{
		private GUIContent _loopingLabel = new GUIContent("Looping");
		private GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
		private SerializedProperty[] _loopingToggles = new SerializedProperty[2];

		private float[] _seamlessSettingRectRatio = new float[] { 0.2f, 0.25f, 0.2f, 0.2f, 0.15f };

        private SerializedProperty _loopProp => _loopingToggles[0];
        private SerializedProperty _seamlessLoopProp => _loopingToggles[1];

		private int GetAdditionalBaseProtiesLineCount(SerializedProperty property, BroAudioType audioType)
		{
			if (!EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
			{
				return default;
			}

			int filterRange = int.MaxValue ^ (DrawedPropertyConstant.AdditionalPropertyStartPoint - 1);
			int count = GetFlagsOnCount((int)setting.DrawedProperty & filterRange);

			return count;
		}

		private int GetFlagsOnCount(int flags)
		{
			int count = 0;
			while (flags != 0)
			{
				flags = flags & (flags - 1);
				count++;
			}
			return count;
		}

		private int GetAdditionalClipPropertiesLineCount(SerializedProperty property, BroAudioType audioType)
		{
			return default;
		}

		private void DrawAdditionalBaseProperties(Rect position, SerializedProperty property, BroAudioType audioType)
		{
			if (!EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
			{
				return;
			}

			DrawDelayProperty(position, property, setting);
			DrawLoopProperty(position, property, setting);

			void DrawDelayProperty(Rect position, SerializedProperty property, EditorSetting.AudioTypeSetting setting)
			{
				if (setting.DrawedProperty.HasFlag(DrawedProperty.Delay))
				{
					SerializedProperty delayProperty = GetBackingNameAndFindProperty(property,nameof(AudioLibrary.Delay));
					delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
				}
			}

			void DrawLoopProperty(Rect position, SerializedProperty property, EditorSetting.AudioTypeSetting setting)
			{
				if (setting.DrawedProperty.HasFlag(DrawedProperty.Loop))
				{
					_loopingToggles[0] = GetBackingNameAndFindProperty(property,nameof(AudioLibrary.Loop));
					_loopingToggles[1] = GetBackingNameAndFindProperty(property,nameof(AudioLibrary.SeamlessLoop));

					Rect loopRect = GetRectAndIterateLine(position);
					DrawToggleGroup(loopRect, _loopingLabel, _loopingToggles);

					if (_loopingToggles[1].boolValue)
					{
						DrawSeamlessSetting(position, property);
					}
				}
			}
		}

		private void DrawAdditionalClipProperties(Rect position, SerializedProperty property, BroAudioType audioType)
		{

		}

		private void DrawSeamlessSetting(Rect totalPosition, SerializedProperty property)
		{
			Rect suffixRect = EditorGUI.PrefixLabel(GetRectAndIterateLine(totalPosition), _seamlessLabel);
			if (!TrySplitRectHorizontal(suffixRect, _seamlessSettingRectRatio, 10f, out Rect[] rects))
			{
				return;
			}
			int drawIndex = 0;
			EditorGUI.LabelField(rects[drawIndex], "Transition By");
			drawIndex++;

			var seamlessTypeProp = property.FindPropertyRelative(NameOf.SeamlessType);
			SeamlessType currentType = (SeamlessType)seamlessTypeProp.enumValueIndex;
			currentType = (SeamlessType)EditorGUI.EnumPopup(rects[drawIndex], currentType);
			seamlessTypeProp.enumValueIndex = (int)currentType;
			drawIndex++;

			var transitionTimeProp = GetBackingNameAndFindProperty(property,nameof(AudioLibrary.TransitionTime));
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
					transitionTimeProp.floatValue = AudioPlayer.UseLibraryManagerSetting;
					break;
			}
		}

		private SerializedProperty GetBackingNameAndFindProperty(SerializedProperty libraryProp, string memberName)
		{
			return libraryProp.FindPropertyRelative(GetBackingFieldName(memberName));
		}
	} 
}
