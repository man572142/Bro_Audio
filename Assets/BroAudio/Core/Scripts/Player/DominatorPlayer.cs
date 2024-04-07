using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class DominatorPlayer : AudioPlayerDecorator , IPlayerEffect
	{
        public BroAudioType DominatedType { get; private set; }


        #region Quiet Others
        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime)
        {
            return this.QuietOthers(othersVol, new Fading(fadeTime, EffectType.Volume));
        }

        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, Fading fading)
        {
            if (othersVol <= 0f || othersVol > 1f)
            {
                LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
                return this;
            }

            SetAllEffectExceptDominator(new Effect(EffectType.Volume, othersVol, fading, true));
            return this;
        }
        #endregion

        #region LowPass Others
        IPlayerEffect IPlayerEffect.LowPassOthers(float freq, float fadeTime)
        {
            return this.LowPassOthers(freq, new Fading(fadeTime, EffectType.LowPass));
        }

        IPlayerEffect IPlayerEffect.LowPassOthers(float freq, Fading fading)
        {
            if (!AudioExtension.IsValidFrequency(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(new Effect(EffectType.LowPass, freq, fading, true));
            return this;
        }
        #endregion

        #region HighPass Others
        IPlayerEffect IPlayerEffect.HighPassOthers(float freq, float fadeTime)
        {
            return this.HighPassOthers(freq, new Fading(fadeTime, EffectType.HighPass));
        }

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq, Fading fading)
        {
            if (!AudioExtension.IsValidFrequency(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(new Effect(EffectType.HighPass, freq, fading, true));
            return this;
        } 
        #endregion

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