using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio
{
	[System.Serializable]
	public class BroAudioClip
	{
		public AudioClip OriginAudioClip;
		public AudioClip EditedAudioClip;
		[Range(0f, 1f)] public float Volume;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public int Weight;

		public AudioClip AudioClip
		{
			get
			{
				if (EditedAudioClip != null)
				{
					return EditedAudioClip;
				}
				else
				{
					return OriginAudioClip;
				}
			}
		}

		public bool IsNull() => AudioClip == null;

		public static void ResetAllSerializedProperties(SerializedProperty property)
		{
			property.FindPropertyRelative(nameof(OriginAudioClip)).objectReferenceValue = null;
			property.FindPropertyRelative(nameof(EditedAudioClip)).objectReferenceValue = null;
			property.FindPropertyRelative(nameof(Volume)).floatValue = 1f;
			property.FindPropertyRelative(nameof(StartPosition)).floatValue = 0f;
			property.FindPropertyRelative(nameof(EndPosition)).floatValue = 0f;
			property.FindPropertyRelative(nameof(FadeIn)).floatValue = 0f;
			property.FindPropertyRelative(nameof(FadeOut)).floatValue = 0f;

			property.FindPropertyRelative(nameof(Weight)).intValue = 0;
		}
	}

}