using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;

namespace Ami.BroAudio.Runtime
{
    public struct PlaybackPreference
    {
        public readonly IAudioEntity Entity;
        private readonly Vector3 _position;
        private readonly Transform _followTarget;

        private int _velocity;
        public float FadeIn { get; set; }
        public float FadeOut { get; set; }
        public Ease FadeInEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeIn : SoundManager.FadeInEase;
        public Ease FadeOutEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeOut : SoundManager.FadeOutEase;

        public PlaybackPreference(IAudioEntity entity, Vector3 position) : this(entity)
        {
            _position = position;
        }

        public PlaybackPreference(IAudioEntity entity, Transform followTarget) : this(entity)
        {
            _followTarget = followTarget;
        }

        public PlaybackPreference(IAudioEntity entity)
        {
            Entity = entity;
            FadeIn = UseEntitySetting;
            FadeOut = UseEntitySetting;
            _position = Vector3.negativeInfinity;
            _followTarget = null;
            _velocity = default;
        }

        public BroAudioClip PickNewClip()
        {
            return Entity.PickNewClip(_velocity);
        }

        public void ResetFading()
        {
            SetFadeTime(Transition.Immediate, 0f);
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

        public void SetVelocity(int velocity)
        {
            _velocity = velocity;
        }

        public bool HasFollowTarget(out Transform target)
        {
            target = _followTarget;
            return target != null;
        }

        public bool HasPosition(out Vector3 position)
        {
            position = _position;
            return !_position.Equals(Vector3.negativeInfinity);
        }
    }
}