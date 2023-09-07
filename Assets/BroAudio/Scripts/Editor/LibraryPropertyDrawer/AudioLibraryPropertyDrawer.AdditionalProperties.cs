using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioLibrary))]
	public partial class AudioLibraryPropertyDrawer : MiPropertyDrawer
	{
		private int GetAdditionalBaseProtiesLineCount(SerializedProperty property, BroAudioType audioType)
		{
			return default;
		}

		private int GetAdditionalClipPropertiesLineCount(SerializedProperty property, BroAudioType audioType)
		{
			return default;
		}

		private void DrawAdditionalBaseProperties(Rect position, SerializedProperty property, BroAudioType audioType)
		{


			SerializedProperty delayProperty = property.FindPropertyRelative(nameof(AudioLibrary.Delay));
			delayProperty.floatValue = EditorGUI.FloatField(GetRectAndIterateLine(position), "Delay", delayProperty.floatValue);
		}

		private void DrawAdditionalClipProperties(Rect position, SerializedProperty property, BroAudioType audioType)
		{

		}
	} 
}
