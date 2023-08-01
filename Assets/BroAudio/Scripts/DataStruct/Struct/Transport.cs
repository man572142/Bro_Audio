using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor 
{
	public struct Transport
	{
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public float FullLength;

		public Transport(BroAudioClip clip)
		{
			StartPosition = clip.StartPosition;
			EndPosition = clip.EndPosition;
			FadeIn = clip.FadeIn;
			FadeOut = clip.FadeOut;
            FullLength = 0f;
            if (clip.AudioClip)
			{
                FullLength = clip.AudioClip.length;
            }
		}

		public bool HasDifferentPosition => StartPosition != 0f || EndPosition != 0f;
		public bool HasFading => FadeIn != 0f || FadeOut != 0f;
	}
}