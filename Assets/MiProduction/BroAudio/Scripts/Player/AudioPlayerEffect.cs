using MiProduction.Extension;
using UnityEngine;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
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

			Player.SetEffectMode(true);

			float targetVolInDb = AudioExtension.ToDecibel(othersVol);
			EffectParameter parameter = new EffectParameter()
			{
				Value = targetVolInDb,
				FadeTime = fadeTime,
				Type = EffectType.Volume
			};

			SoundManager.Instance.SetEffectTrackParameter(parameter).While(PlayerIsPlaying);
			return this;
		}

		IPlayerEffect IPlayerEffect.LowPassOthers(float freq ,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetEffectMode(true);

            EffectParameter parameter = new EffectParameter()
            {
                Value = freq,
                FadeTime = fadeTime,
                Type = EffectType.LowPass
            };

            SoundManager.Instance.SetEffectTrackParameter(parameter).While(PlayerIsPlaying);
            return this;
        }

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq,float fadeTime)
        {
            if (!AudioExtension.IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetEffectMode(true);

            EffectParameter parameter = new EffectParameter()
            {
                Value = freq,
                FadeTime = fadeTime,
                Type = EffectType.HighPass
            };

            SoundManager.Instance.SetEffectTrackParameter(parameter).While(PlayerIsPlaying);
            return this;
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