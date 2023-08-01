using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Runtime
{
	public struct PlaybackPreference
	{
		public readonly bool IsNormalLoop;
		public readonly bool IsSeamlessLoop;
		public readonly float Delay;

		public readonly Vector3 Position;
		public readonly Transform FollowTarget;

		public readonly Ease FadeInEase;
		public readonly Ease FadeOutEase;
		public readonly float SeamlessTransitionTime;

		public float FadeIn { get; private set; }
		public float FadeOut { get; private set; }
		public bool HaveToWaitForPrevious { get; private set; }

		public PlaybackPreference(IAudioLibrary library,Vector3 position) : this(library)
		{
			Position = position;
		}

		public PlaybackPreference(IAudioLibrary library, Transform followTarget) : this(library)
		{
			FollowTarget = followTarget;
		}

		public PlaybackPreference(IAudioLibrary library)
		{
			FadeIn = UseLibraryManagerSetting;
			FadeOut = UseLibraryManagerSetting;
			SeamlessTransitionTime = UseLibraryManagerSetting;
			IsSeamlessLoop = false;
			IsNormalLoop = false;
			HaveToWaitForPrevious = false;
			Delay = default;
			Position = Vector3.negativeInfinity;
			FollowTarget = null;

            BroAudioType audioType = GetAudioType(library.ID);

			if(PersistentType.HasFlag(audioType))
			{
				var persistentLib = library.CastTo<PersistentAudioLibrary>();
				SeamlessTransitionTime = persistentLib.TransitionTime;
                IsSeamlessLoop = persistentLib.SeamlessLoop ;
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
			FadeIn = SeamlessTransitionTime;
			FadeOut = SeamlessTransitionTime;
		}

		public void WaitForPrevious(bool enable)
		{
			HaveToWaitForPrevious = enable;
		}

		public bool HasPosition(out Vector3 position)
		{
			position = Position;
			return !Position.Equals(Vector3.negativeInfinity);
		}

		public bool HasFollowTarget(out Transform followTarget)
		{
			followTarget = FollowTarget;
			return FollowTarget != null;
		}
	}
}