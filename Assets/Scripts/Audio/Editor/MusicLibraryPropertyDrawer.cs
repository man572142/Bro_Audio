using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(MusicLibrary))]
	public class MusicLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{
			//SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");
			//SerializedProperty endPosProperty = property.FindPropertyRelative("EndPosition");
			//SerializedProperty fadeInProperty = property.FindPropertyRelative("FadeIn");
			//SerializedProperty fadeOutProperty = property.FindPropertyRelative("FadeOut");

			//_playbackValues[0] = startPosProperty.floatValue;
			//_playbackValues[1] = endPosProperty.floatValue;
			//_fadeValues[0] = fadeInProperty.floatValue;
			//_fadeValues[1] = fadeOutProperty.floatValue;
			//_clipLength = clipLength;

			//EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Playback Position"), _playbackLabels, _playbackValues);
			//startPosProperty.floatValue = Mathf.Clamp(_playbackValues[0], 0f, GetLengthLimit(0,endPosProperty.floatValue,fadeInProperty.floatValue, fadeOutProperty.floatValue));
			//_playbackValues[0] = startPosProperty.floatValue;
			//endPosProperty.floatValue = Mathf.Clamp(_playbackValues[1], 0f, GetLengthLimit(startPosProperty.floatValue,0f, fadeInProperty.floatValue, fadeOutProperty.floatValue));
			//_playbackValues[1] = endPosProperty.floatValue;

			//EditorGUI.MultiFloatField(GetRectAndIterateLine(position),new GUIContent("Fade") ,_fadeLabels, _fadeValues);
			//fadeInProperty.floatValue = Mathf.Clamp(_fadeValues[0],0f, GetLengthLimit(startPosProperty.floatValue, endPosProperty.floatValue, 0f, fadeOutProperty.floatValue));
			//_fadeValues[0] = fadeInProperty.floatValue;
			//fadeOutProperty.floatValue = Mathf.Clamp(_fadeValues[1],0f, GetLengthLimit(startPosProperty.floatValue, endPosProperty.floatValue, fadeInProperty.floatValue, 0f));
			//_fadeValues[1] = fadeOutProperty.floatValue;
			////Debug.Log(GetLengthLimit(_fadeValues[0]));
		}

		protected override Vector3[] GetClipLinePoints(float width)
		{
			if(width <= 0f)
				return new Vector3[0];

			Vector3[] points = new Vector3[4];
			points[0] = new Vector3(Mathf.Lerp(0f, width, PlaybackValues[0] / ClipLength), ClipViewHeight, 0f);
			points[1] = new Vector3(Mathf.Lerp(0f, width, (PlaybackValues[0] + FadeValues[0]) / ClipLength), 0f, 0f);
			points[2] = new Vector3(Mathf.Lerp(0f, width, (ClipLength - PlaybackValues[1] - FadeValues[1]) / ClipLength), 0f, 0f);
			points[3] = new Vector3(Mathf.Lerp(0f, width, (ClipLength - PlaybackValues[1]) / ClipLength), ClipViewHeight, 0f);

			//Debug.Log($"{points[0]},{points[1]},{points[2]},{points[3]}");
			return points;
		}

		
	}
}
