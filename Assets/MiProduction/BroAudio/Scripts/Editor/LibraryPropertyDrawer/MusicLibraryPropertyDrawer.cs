using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(MusicLibrary))]
	public class MusicLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		protected override int BasePropertiesLineCount => 3;

		protected override int ClipPropertiesLineCount => 4;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty loopProperty = property.FindPropertyRelative(nameof(MusicLibrary.Loop));
			loopProperty.boolValue = EditorGUI.Toggle(GetRectAndIterateLine(position),"Loop", loopProperty.boolValue);
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

		

		
	}
}
