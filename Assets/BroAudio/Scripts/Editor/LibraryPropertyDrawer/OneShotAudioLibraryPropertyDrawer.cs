using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public class OneShotAudioLibraryPropertyDrawer
	{
		// The number should match the amount of EditorGUI elements that being draw in this script.
		int GetAdditionalBaseProtiesLineCount(SerializedProperty property) => 1;
		int GetAdditionalClipPropertiesLineCount(SerializedProperty property) => 0;

		void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			//SerializedProperty delayProperty = property.FindPropertyRelative(nameof(AudioLibrary.Delay));
			//delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
		}

		void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

	}
}
