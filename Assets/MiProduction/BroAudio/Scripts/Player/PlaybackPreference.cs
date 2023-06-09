using static MiProduction.BroAudio.Runtime.AudioPlayer;

namespace MiProduction.BroAudio.Runtime
{
	public struct PlaybackPreference
	{
		public readonly bool IsLoop;
		public readonly float Delay;

		public float FadeIn;
		public float FadeOut;

		public PlaybackPreference(bool isLoop, float delay)
		{
			IsLoop = isLoop;
			Delay = delay;

			FadeIn = UseClipFadeSetting;
			FadeOut = UseClipFadeSetting;
		}

		public PlaybackPreference(bool isLoop)
		{
			IsLoop = isLoop;
			Delay = 0f;
			FadeIn = UseClipFadeSetting;
			FadeOut = UseClipFadeSetting;
		}

		public PlaybackPreference(float delay)
		{
			IsLoop = false;
			Delay = delay;
			FadeIn = UseClipFadeSetting;
			FadeOut = UseClipFadeSetting;
		}

		public void SetFadeTime(Transition transition,float fadeTime)
		{
			switch (transition)
			{
				case Transition.Immediate:
					FadeIn = 0f;
					FadeOut = 0f;
					break;
				case Transition.OnlyFadeIn:
					SetIfOverride(fadeTime, ref FadeIn);
					FadeOut = 0f;
					break;
				case Transition.OnlyFadeOut:
					FadeIn = 0f;
					SetIfOverride(fadeTime, ref FadeOut);
					break;
				case Transition.Default:
				case Transition.CrossFade:
					SetIfOverride(fadeTime, ref FadeIn);
					SetIfOverride(fadeTime, ref FadeOut);
					break;
			}

			void SetIfOverride(float overrideTime,ref float fade)
			{
				if(overrideTime != UseClipFadeSetting)
				{
					fade = overrideTime;
				}
			}
		}

		public static PlaybackPreference GetEmptyPreference()
		{
			return new PlaybackPreference(false,0f);
		}
	}
}