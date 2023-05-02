using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio.Data
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
		
	}

}