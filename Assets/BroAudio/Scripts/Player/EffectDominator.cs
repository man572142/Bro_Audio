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

			SetAllEffectExceptDominator(Effect.Volume(othersVol,fadeTime));
			return this;
		}

		IPlayerEffect IPlayerEffect.HighCutOthers(float freq ,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(Effect.HighCut(freq,fadeTime));
            return this;
        }

        IPlayerEffect IPlayerEffect.LowCutOthers(float freq,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            SetAllEffectExceptDominator(Effect.LowCut(freq,fadeTime));
            return this;
        }

        internal void SetDominatedType(BroAudioType dominatdType)
        {
            DominatedType = dominatdType;
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
                return Player.IsPlaying;
            }
            return false;
		}
    }
}