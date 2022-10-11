using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class SoundLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{

		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{
   //         SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");
   //         SerializedProperty endPosProperty = property.FindPropertyRelative("EndPosition");

			//_playbackValues[0] = startPosProperty.floatValue;
			//_playbackValues[1] = endPosProperty.floatValue;
			//_clipLength = clipLength;

			//EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Playback Position"), _playbackLabels, _playbackValues);
			//startPosProperty.floatValue = Mathf.Clamp(_playbackValues[0], 0f, GetLengthLimit(_playbackValues[0]));
			//endPosProperty.floatValue = Mathf.Clamp(_playbackValues[1], 0f, GetLengthLimit(_playbackValues[1]));
		}

		protected override Vector3[] GetClipLinePoints(float width)
		{
			float startPoint = Mathf.Lerp(0f, width, PlaybackValues[0] / ClipLength);
			float endPoint = Mathf.Lerp(0f, width, ClipLength - PlaybackValues[1] / ClipLength);
			Vector3[] points = new Vector3[4];
			points[0] = new Vector3(startPoint, ClipViewHeight, 0f);
			points[1] = new Vector3(startPoint, 0f, 0f);
            points[2] = new Vector3(endPoint, 0f, 0f);
            points[3] = new Vector3(endPoint, ClipViewHeight, 0f);

            //Debug.Log($"{points[0]},{points[1]},{points[2]},{points[3]}");
            return points;
		}
	}
}
