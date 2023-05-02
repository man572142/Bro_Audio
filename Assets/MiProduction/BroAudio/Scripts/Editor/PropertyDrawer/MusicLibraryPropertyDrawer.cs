using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.AssetEditor
{
	[CustomPropertyDrawer(typeof(Data.MusicLibrary))]
	public class MusicLibraryPropertyDrawer : AudioLibraryPropertyDrawer
	{
		protected override int BasePropertiesLineCount => 3;

		protected override int ClipPropertiesLineCount => 4;

		protected override void DrawAdditionalBaseProperties(Rect position, SerializedProperty property)
		{
			SerializedProperty loopProperty = property.FindPropertyRelative("Loop");
			loopProperty.boolValue = EditorGUI.Toggle(GetRectAndIterateLine(position),"Loop", loopProperty.boolValue);
		}

		protected override void DrawAdditionalClipProperties(Rect position, SerializedProperty property)
		{

		}

		

		
	}
}
