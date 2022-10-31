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

	}
}
