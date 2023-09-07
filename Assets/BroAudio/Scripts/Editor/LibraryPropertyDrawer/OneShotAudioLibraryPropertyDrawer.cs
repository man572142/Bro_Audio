using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public class OneShotAudioLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		// The number should match the amount of EditorGUI elements that being draw in this script.
		protected override int GetAdditionalBaseProtiesLineCount(SerializedProperty property) => 1;
		protected override int GetAdditionalClipPropertiesLineCount(SerializedProperty property) => 0;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty delayProperty = property.FindPropertyRelative(nameof(AudioLibrary.Delay));
			delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

	}
}
