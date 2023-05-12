using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.AssetEditor
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class SoundLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		protected override int BasePropertiesLineCount => 3;

		protected override int ClipPropertiesLineCount => 4;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty delayProperty = property.FindPropertyRelative(nameof(SoundLibrary.Delay));
			delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

	}
}
