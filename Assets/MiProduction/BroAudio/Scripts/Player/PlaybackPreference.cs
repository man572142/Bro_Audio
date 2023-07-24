using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Runtime.AudioPlayer;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
	public class PlaybackPreference
	{
		public readonly bool IsLoop = false;
		public readonly bool IsSeamlessLoop = false;
		public readonly float Delay = 0f;

		public float FadeIn = UseLibraryManagerSetting;
		public float FadeOut = UseLibraryManagerSetting;

		public Ease FadeInEase = Ease.Linear;
		public Ease FadeOutEase = Ease.Linear;

		public bool HaveToWaitForPrevious = false;

		public PlaybackPreference(IAudioLibrary library,float fadeOut)
		{
			BroAudioType audioType = GetAudioType(library.ID);

			if(PersistentType.HasFlag(audioType))
			{
				var persistentLib = library.CastTo<PersistentAudioLibrary>();
				IsLoop = persistentLib.Loop ;
				if (persistentLib.SeamlessLoop)
				{
                    if (persistentLib.TransitionTime >= 0)
					{
                        //HACK: 這樣會讓第一次播放失去自己的FadeIn
                        FadeIn = persistentLib.TransitionTime;
                        FadeOut = persistentLib.TransitionTime;
						IsSeamlessLoop = persistentLib.TransitionTime != 0;
						IsLoop = !IsSeamlessLoop;
                    }
					else if(persistentLib.TransitionTime < 0)
					{
						IsSeamlessLoop = fadeOut > 0;
						IsLoop = !IsSeamlessLoop;
					}
				}
			}
			else if(OneShotType.HasFlag(audioType))
			{
				Delay = library.CastTo<OneShotAudioLibrary>().Delay;
			}

			FadeInEase = IsSeamlessLoop ? SoundManager.SeamlessFadeIn : SoundManager.FadeInEase;
			FadeOutEase = IsSeamlessLoop ? SoundManager.SeamlessFadeOut : SoundManager.FadeOutEase;
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
	}
}