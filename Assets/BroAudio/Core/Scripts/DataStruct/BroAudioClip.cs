using UnityEngine;

namespace Ami.BroAudio.Data
{
	[System.Serializable]
	public class BroAudioClip : IBroAudioClip
	{
		public AudioClip AudioClip;
		public float Volume;
		public float Delay;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		// For random, velocity
		public int Weight;

		// For shuffle (runtime-only)
		[System.NonSerialized]
		public bool IsUsed;

		AudioClip IBroAudioClip.AudioClip => AudioClip;
		float IBroAudioClip.Volume => Volume;
		float IBroAudioClip.Delay => Delay;
		float IBroAudioClip.StartPosition => StartPosition;
		float IBroAudioClip.EndPosition => EndPosition;
		float IBroAudioClip.FadeIn => FadeIn;
		float IBroAudioClip.FadeOut => FadeOut;
        public int Velocity => Weight;
		public bool IsNull() => AudioClip == null;
	}

	public interface IBroAudioClip
	{
		AudioClip AudioClip { get; }
		float Volume { get; }
		float Delay { get; }
		float StartPosition { get; }
		float EndPosition { get; }
		float FadeIn { get;}
		float FadeOut { get; }
	}
}