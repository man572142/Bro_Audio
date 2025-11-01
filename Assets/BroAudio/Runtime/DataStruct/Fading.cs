using Ami.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
    public struct Fading
    {
        public float FadeIn;
        public float FadeOut;
        public Ease FadeInEase;
        public Ease FadeOutEase;

        public Fading(float fadeIn, float fadeOut, EffectType effectType) : this(effectType)
        {
            FadeIn = fadeIn;
            FadeOut = fadeOut;
        }

        public Fading(float fadeTime, EffectType effectType) : this(effectType)
        {
            FadeIn = fadeTime;
            FadeOut = fadeTime;
        }

        public Fading(EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.LowPass:
                    FadeInEase = BroAdvice.LowPassInEase;
                    FadeOutEase = BroAdvice.LowPassOutEase;
                    break;
                case EffectType.HighPass:
                    FadeInEase = BroAdvice.HighPassInEase;
                    FadeOutEase = BroAdvice.HighPassOutEase;
                    break;
                default:
                    FadeInEase = Ease.Linear;
                    FadeOutEase = Ease.Linear;
                    break;
            }
            FadeIn = default;
            FadeOut = default;
        }

        public Fading(float fadeIn, float fadeOut, Ease fadeInEase, Ease fadeOutEase)
        {
            FadeOut = fadeOut;
            FadeIn = fadeIn;
            FadeInEase = fadeInEase;
            FadeOutEase = fadeOutEase;
        }
    }
}