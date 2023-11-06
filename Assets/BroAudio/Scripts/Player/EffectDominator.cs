using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class EffectDominator : AudioPlayerDecorator , IPlayerEffect
	{
        public BroAudioType DominatedType { get; private set; }

		IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime)
		{
			if (othersVol <= 0f || othersVol > 1f)
			{
				LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
				return this;
			}

			EffectParameter effect = new EffectParameter(EffectType.Volume)
			{
				Value = othersVol,
				FadeTime = fadeTime,
			};

			SetAllEffectExceptDominator(effect);
			return this;
		}

		IPlayerEffect IPlayerEffect.HighCutOthers(float freq ,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            EffectParameter effect = new EffectParameter(EffectType.HighCut)
            {
                Value = freq,
                FadeTime = fadeTime,
            };

            SetAllEffectExceptDominator(effect);
            return this;
        }

        IPlayerEffect IPlayerEffect.LowCutOthers(float freq,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            EffectParameter effect = new EffectParameter(EffectType.LowCut)
            {
                Value = freq,
                FadeTime = fadeTime,
            };

            SetAllEffectExceptDominator(effect);
            return this;
        }

        internal void SetDominatedType(BroAudioType dominatdType)
        {
            DominatedType = dominatdType;
        }

        private void SetAllEffectExceptDominator(EffectParameter effect)
        {
           
            SoundManager.Instance.SetEffect(DominatedType, effect).While(PlayerIsPlaying);
            Player.SetEffect(EffectType.None,SetEffectMode.Override);
        }

        private bool PlayerIsPlaying()
		{
            if(Player != null)
			{
                return Player.IsPlaying;
            }
            return false;
		}
    }
}