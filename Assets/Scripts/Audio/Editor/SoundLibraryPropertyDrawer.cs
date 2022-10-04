using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class SoundLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		private float _startPos = 0f;
		private float _endPos = 0f;
		private float _clipLength = 0f;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{

		}

		protected override void DrawClipProperties(Rect position, SerializedProperty property,float clipLength)
		{
            SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");
            SerializedProperty endPosProperty = property.FindPropertyRelative("EndPosition");

			_startPos = startPosProperty.floatValue;
			_endPos = endPosProperty.floatValue;
			_clipLength = clipLength;

            // Start Position
            startPosProperty.floatValue =
                Mathf.Clamp(EditorGUI.FloatField(GetRectAndIterateLine(position), "Start Position", startPosProperty.floatValue), 0f, clipLength - endPosProperty.floatValue);
            // End Position
            endPosProperty.floatValue =
                Mathf.Clamp(EditorGUI.FloatField(GetRectAndIterateLine(position), "End Position", endPosProperty.floatValue), 0f, clipLength - startPosProperty.floatValue);
        }

		protected override Vector3[] GetClipLinePoints(float width)
		{
			float startPoint = Mathf.Lerp(0f, width, _startPos / _clipLength);
			float endPoint = Mathf.Lerp(0f, width, _clipLength - _endPos / _clipLength);
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
