using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[System.Serializable]
	public class BroAudioClip
	{
		public AudioClip AudioClip;
		[Range(0f, 1f)] public float Volume;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public int Weight;

		public bool IsNull() => AudioClip == null;
		
	}
}