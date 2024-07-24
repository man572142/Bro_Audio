using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;

namespace Ami.BroAudio.Runtime
{
    public interface IAdditionalStrategy
    {
        bool TryGetAdditionalValue<T>(out T value);
        bool IsTupleValue();
    }

    public class AdditionalStrategy<TStrategy> : IAdditionalStrategy
    {
        private TStrategy _value;

        public AdditionalStrategy(TStrategy value)
        {
            _value = value;
        }

        public bool IsTupleValue()
        {
            return _value is System.ValueTuple;
        }

        public bool TryGetAdditionalValue<TValue>(out TValue value)
        {
            value = default(TValue);
            if(_value is TValue castedVal)
            {
                value = castedVal;
                return true;
            }
            Debug.LogError(Utility.LogTitle + $"the target value should be type of :{typeof(TStrategy)}");
            return false;
        }
    }

    public struct PlaybackPreference
    {
        public readonly IAudioEntity Entity;

        private IAdditionalStrategy _additionalStrategy;

        public float FadeIn { get; private set; }
        public float FadeOut { get; private set; }
        public Ease FadeInEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeIn : SoundManager.FadeInEase;
        public Ease FadeOutEase => Entity.SeamlessLoop ? SoundManager.SeamlessFadeOut : SoundManager.FadeOutEase;

        public PlaybackPreference(IAudioEntity entity, Vector3 position) : this(entity)
        {
            _additionalStrategy = new AdditionalStrategy<Vector3>(position);
        }

        public PlaybackPreference(IAudioEntity entity, Transform followTarget) : this(entity)
        {
            _additionalStrategy = new AdditionalStrategy<Transform>(followTarget);
        }

        public PlaybackPreference(IAudioEntity entity)
        {
            Entity = entity;
            FadeIn = UseEntitySetting;
            FadeOut = UseEntitySetting;
            _additionalStrategy = null;
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

        public bool HasPosition(out Vector3 position)
        {
            position = default;
            return _additionalStrategy != null && _additionalStrategy.TryGetAdditionalValue(out position);
        }

        public bool HasFollowTarget(out Transform target)
        {
            target = null;
            return _additionalStrategy != null && _additionalStrategy.TryGetAdditionalValue(out target);
        }
    }
}