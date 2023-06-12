using static MiProduction.BroAudio.Runtime.AudioPlayer;

namespace MiProduction.BroAudio.Runtime
{
	public class PlaybackPreference
	{
		public readonly bool IsLoop = false;
		public readonly float Delay = 0f;

		public float FadeIn = UseLibraryManagerSetting;
		public float FadeOut = UseLibraryManagerSetting;

		public bool HaveToWaitForPrevious = false;

		public PlaybackPreference(bool isLoop, float delay)
		{
			IsLoop = isLoop;
			Delay = delay;
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
					FadeIn = fadeTime;
					FadeOut = 0f;
					break;
				case Transition.OnlyFadeOut:
					FadeIn = 0f;
					FadeOut = fadeTime;
					break;
				case Transition.Default:
				case Transition.CrossFade:
					FadeIn = fadeTime;
					FadeOut = fadeTime;
					break;
			}
		}

		public static PlaybackPreference GetEmptyPreference()
		{
			return new PlaybackPreference(false,0f);
		}
	}
}