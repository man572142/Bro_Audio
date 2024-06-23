using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;

namespace Ami.BroAudio.Runtime
{
	public struct PlaybackPreference
	{
		public readonly IAudioEntity Entity;

		public readonly Vector3 Position;
		public readonly Transform FollowTarget;

		public Ease FadeInEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeIn : SoundManager.FadeInEase;
		public Ease FadeOutEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeOut : SoundManager.FadeOutEase;

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
			Entity = entity;
			FadeIn = UseEntitySetting;
			FadeOut = UseEntitySetting;
			Position = Vector3.negativeInfinity;
			FollowTarget = null;
			PlayerWaiter = null;
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
			FadeIn = Entity.TransitionTime;
			FadeOut = Entity.TransitionTime;
		}

		public Waiter CreateWaiter()
		{
			PlayerWaiter = PlayerWaiter ?? new Waiter();
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
	}
}