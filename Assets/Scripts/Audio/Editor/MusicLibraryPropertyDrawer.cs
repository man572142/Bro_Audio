using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(MusicLibrary))]
	public class MusicLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		private float _fadeIn = 0f;
		private float _fadeOut = 0f;
		private float _startPos = 0f;
		private float _endPos = 0f;
		private float _clipLength = 0f;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			
		}

		protected override void DrawClipProperties(Rect position, SerializedProperty property,float clipLength)
		{
			SerializedProperty fadeInProperty = property.FindPropertyRelative("FadeIn");
			SerializedProperty fadeOutProperty = property.FindPropertyRelative("FadeOut");
            SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");
            SerializedProperty endPosProperty = property.FindPropertyRelative("EndPosition");

            _fadeIn = fadeInProperty.floatValue;
            _fadeOut = fadeOutProperty.floatValue;
            _startPos = startPosProperty.floatValue;
            _endPos = endPosProperty.floatValue;
			_clipLength = clipLength;

            fadeInProperty.floatValue = Mathf.Clamp(
				EditorGUI.FloatField(GetRectAndIterateLine(position), "Fade In", _fadeIn),0f,GetLengthLimit(_fadeIn));
			fadeOutProperty.floatValue = Mathf.Clamp(
				EditorGUI.FloatField(GetRectAndIterateLine(position), "Fade Out", _fadeOut),0f,GetLengthLimit(_fadeOut));
            startPosProperty.floatValue = Mathf.Clamp(
                EditorGUI.FloatField(GetRectAndIterateLine(position), "Start Position", _startPos), 0f, GetLengthLimit(_startPos));
            endPosProperty.floatValue = Mathf.Clamp(
                EditorGUI.FloatField(GetRectAndIterateLine(position), "End Position", _endPos), 0f,GetLengthLimit(_endPos));

            
        }

		protected override Vector3[] GetClipLinePoints(float width)
		{
			if(width <= 0f)
				return new Vector3[0];

			Vector3[] points = new Vector3[4];
			points[0] = new Vector3(Mathf.Lerp(0f, width, _startPos / _clipLength), ClipViewHeight, 0f);
			points[1] = new Vector3(Mathf.Lerp(0f, width, (_startPos + _fadeIn) / _clipLength), 0f, 0f);
			points[2] = new Vector3(Mathf.Lerp(0f, width, (_clipLength - _endPos - _fadeOut) / _clipLength), 0f, 0f);
			points[3] = new Vector3(Mathf.Lerp(0f, width, (_clipLength - _endPos) / _clipLength), ClipViewHeight, 0f);

			//Debug.Log($"{points[0]},{points[1]},{points[2]},{points[3]}");
			return points;
		}

		private float GetLengthLimit(float self)
		{
			return _clipLength - _startPos - _fadeIn - _fadeOut - _endPos + self;
		}
	}
}
