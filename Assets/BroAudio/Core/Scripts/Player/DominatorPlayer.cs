using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class DominatorPlayer : AudioPlayerDecorator , IPlayerEffect
	{
        public BroAudioType DominatedType { get; private set; }

		IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime)
		{
			if (othersVol <= 0f || othersVol > 1f)
			{
				LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
				return this;
			}


			SetAllEffectExceptDominator(new Effect(EffectType.Volume, othersVol, fadeTime, Ease.Linear, true));
			return this;
		}

		IPlayerEffect IPlayerEffect.LowPassOthers(float freq ,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequency(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(new Effect(EffectType.LowPass, freq, fadeTime, BroAdvice.LowPassEase, true));
            return this;
        }

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequency(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(new Effect(EffectType.HighPass, freq, fadeTime, BroAdvice.HighPassEase, true));
            return this;
        }

        internal void SetDominatedType(BroAudioType dominatedType)
        {
            DominatedType = dominatedType;
        }

        private void SetAllEffectExceptDominator(Effect effect)
        {
            SoundManager.Instance.SetEffect(DominatedType, effect).While(PlayerIsPlaying);
            Player.SetEffect(EffectType.None,SetEffectMode.Override);
        }

        private bool PlayerIsPlaying()
		{
            if(Player != null)
			{
				return Player.ID > 0;
            }
            return false;
		}
    }
}