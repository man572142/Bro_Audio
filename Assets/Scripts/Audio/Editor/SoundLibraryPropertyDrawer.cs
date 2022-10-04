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
		}

		protected override Vector3[] GetClipLinePoints(float startPos, float clipLength, float width)
		{
			float startPoint = Mathf.Lerp(0f, width, startPos / clipLength);
			Vector3[] points = new Vector3[2];
			points[0] = new Vector3(startPoint, 0f, 0f);
			points[1] = new Vector3(startPoint, ClipViewHeight, 0f);

			return points;
		}
	}
}
