using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Data
{
	[System.Serializable]
	public class BroAudioClip : IBroAudioClip
	{
		public AudioClip AudioClip;
		public float Volume;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public int Weight;

#if UNITY_EDITOR
		public bool SnapToFullVolume = true;
#endif

		AudioClip IBroAudioClip.AudioClip => AudioClip;
		float IBroAudioClip.Volume => Volume;
		float IBroAudioClip.StartPosition => StartPosition;
		float IBroAudioClip.EndPosition => EndPosition;
		float IBroAudioClip.FadeIn => FadeIn;
		float IBroAudioClip.FadeOut => FadeOut;
		public bool IsNull() => AudioClip == null;
	}

	public interface IBroAudioClip
	{
		AudioClip AudioClip { get; }
		float Volume { get; }
		float StartPosition { get; }
		float EndPosition { get; }
		float FadeIn { get;}
		float FadeOut { get; }
	}
}