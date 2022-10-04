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


		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty fadeInProperty = property.FindPropertyRelative("FadeIn");
			SerializedProperty fadeOutProperty = property.FindPropertyRelative("FadeOut");

			fadeInProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Fade In", fadeInProperty.floatValue);
			fadeOutProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Fade Out", fadeOutProperty.floatValue);
		}

		protected override Vector3[] GetClipLinePoints(float startPos,float clipLength,float width)
		{
			float startPoint = Mathf.Lerp(0f,width,startPos / clipLength);
			Vector3[] points = new Vector3[4];
			points[0] = new Vector3(startPoint, ClipViewHeight, 0f);
			points[1] = new Vector3(Mathf.Lerp(0f, width, _fadeIn / clipLength), 0f, 0f);
			points[2] = new Vector3(Mathf.Lerp(0f, width, _fadeOut / clipLength), 0f, 0f);
			points[3] = new Vector3(startPoint, ClipViewHeight, 0f);

			return points;
		}
	}
}
