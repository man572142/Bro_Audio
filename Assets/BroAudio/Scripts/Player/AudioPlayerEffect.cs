using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class AudioPlayerEffect : AudioPlayerDecorator , IPlayerEffect
	{
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

			SetAllEffectExceptMyself(effect);
			return this;
		}

		IPlayerEffect IPlayerEffect.LowPassOthers(float freq ,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            EffectParameter effect = new EffectParameter(EffectType.LowPass)
            {
                Value = freq,
                FadeTime = fadeTime,
            };

            SetAllEffectExceptMyself(effect);
            return this;
        }

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            EffectParameter effect = new EffectParameter(EffectType.HighPass)
            {
                Value = freq,
                FadeTime = fadeTime,
            };

            SetAllEffectExceptMyself(effect);
            return this;
        }

        private void SetAllEffectExceptMyself(EffectParameter effect)
        {
            // set effect for all except this plyer itself
            SoundManager.Instance.SetEffect(BroAudioType.All, effect).While(PlayerIsPlaying);
            Player.SetEffect(effect.Type,SetEffectMode.Remove);
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