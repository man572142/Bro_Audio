using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[System.Serializable]
	public class BroAudioClip
	{
		public AudioClip AudioClip;
		public float Volume;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public int Weight;

#if UNITY_EDITOR
		public bool AllowBoost;

#endif

		public bool IsNull() => AudioClip == null;
		
	}
}