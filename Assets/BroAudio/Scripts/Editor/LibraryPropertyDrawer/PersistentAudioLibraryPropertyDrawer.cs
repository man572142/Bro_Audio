using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using static Ami.Extension.EditorScriptingExtension;
using Ami.Extension;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Data.AudioLibrary;

namespace Ami.BroAudio.Editor
{
	public class PersistentAudioLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		private GUIContent _loopingLabel = new GUIContent("Looping");
		private GUIContent _seamlessLabel = new GUIContent("Seamless Setting");
		private SerializedProperty[] _loopingToggles = new SerializedProperty[2];

		private float[] _seamlessSettingRectRatio = new float[] { 0.2f, 0.25f, 0.2f, 0.2f ,0.15f};

		//The number should match the amount of EditorGUI elements that being draw in this script.
		 int GetAdditionalBaseProtiesLineCount(SerializedProperty property)
		{
			var seamlessToggleProp = property.FindPropertyRelative(nameof(AudioLibrary.SeamlessLoop));
			return seamlessToggleProp.boolValue ? 2 : 1;
		}
		int GetAdditionalClipPropertiesLineCount(SerializedProperty property) => 0;

		void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			_loopingToggles[0] = property.FindPropertyRelative(nameof(AudioLibrary.Loop));
			_loopingToggles[1] = property.FindPropertyRelative(nameof(AudioLibrary.SeamlessLoop));

			Rect loopRect = GetRectAndIterateLine(position);
			DrawToggleGroup(loopRect, _loopingLabel, _loopingToggles);

			if (_loopingToggles[1].boolValue)
			{
				DrawSeamlessSetting(position, property);
			}
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

			var transitionTimeProp = property.FindPropertyRelative(nameof(AudioLibrary.TransitionTime));
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

		void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}
	}
}
