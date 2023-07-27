using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Runtime.AudioPlayer;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
	public class PlaybackPreference
	{
		public readonly bool IsNormalLoop = false;
		public readonly bool IsSeamlessLoop = false;
		public readonly float Delay = 0f;

		public readonly Ease FadeInEase = Ease.Linear;
		public readonly Ease FadeOutEase = Ease.Linear;

		private float _seamlessTransitionTime = UseLibraryManagerSetting;

		public float FadeIn { get; private set; } = UseLibraryManagerSetting;
		public float FadeOut { get; private set; } = UseLibraryManagerSetting;
		public bool HaveToWaitForPrevious { get; set; }

		public PlaybackPreference(IAudioLibrary library,float fadeOut)
		{
			BroAudioType audioType = GetAudioType(library.ID);

			if(PersistentType.HasFlag(audioType))
			{
				var persistentLib = library.CastTo<PersistentAudioLibrary>();
				_seamlessTransitionTime = persistentLib.TransitionTime;
				IsSeamlessLoop = persistentLib.SeamlessLoop && (persistentLib.TransitionTime >= 0 || fadeOut > 0);
				IsNormalLoop = IsSeamlessLoop ? false : persistentLib.Loop;
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

		public void ApplySeamlessFade()
		{
			FadeIn = _seamlessTransitionTime;
			FadeOut = _seamlessTransitionTime;
		}
	}
}