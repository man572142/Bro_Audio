using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;

namespace Ami.BroAudio.Runtime
{
	public struct PlaybackPreference
	{
		public readonly SpatialSettings SpatialSettings;
		public readonly bool IsNormalLoop;
		public readonly bool IsSeamlessLoop;

		public readonly Vector3 Position;
		public readonly Transform FollowTarget;

		public readonly Ease FadeInEase;
		public readonly Ease FadeOutEase;
		public readonly float SeamlessTransitionTime;

		public float FadeIn { get; private set; }
		public float FadeOut { get; private set; }
		public Waiter PlayerWaiter { get; private set; }

		public PlaybackPreference(IAudioEntity entity,Vector3 position) : this(entity)
		{
			Position = position;
		}

		public PlaybackPreference(IAudioEntity entity, Transform followTarget) : this(entity)
		{
			FollowTarget = followTarget;
		}

		public PlaybackPreference(IAudioEntity entity)
		{
			FadeIn = UseEntitySetting;
			FadeOut = UseEntitySetting;
			SeamlessTransitionTime = UseEntitySetting;
			IsSeamlessLoop = false;
			IsNormalLoop = false;
			Position = Vector3.negativeInfinity;
			FollowTarget = null;
			PlayerWaiter = null;

			SeamlessTransitionTime = entity.TransitionTime;
			IsSeamlessLoop = entity.SeamlessLoop;
			IsNormalLoop = IsSeamlessLoop ? false : entity.Loop;
			SpatialSettings = entity.SpatialSettings;

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

		public Waiter CreateWaiter()
		{
			if (PlayerWaiter == null)
			{
                PlayerWaiter = new Waiter();
            }
			
			return PlayerWaiter;
		}

        public void DisposeWaiter()
        {
            PlayerWaiter = null;
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