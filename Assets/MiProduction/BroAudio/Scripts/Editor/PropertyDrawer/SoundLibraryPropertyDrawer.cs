using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library.Core
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class SoundLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty delayProperty = property.FindPropertyRelative("Delay");
			delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

	}
}
