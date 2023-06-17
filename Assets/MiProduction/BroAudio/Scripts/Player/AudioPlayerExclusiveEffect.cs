using MiProduction.Extension;
using UnityEngine;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
	public class AudioPlayerExclusiveEffect : AudioPlayerDecorator , IPlayerExclusive
	{
		IPlayerExclusive IPlayerExclusive.DuckOthers(float othersVol, float fadeTime)
		{
			if (othersVol <= 0f || othersVol > 1f)
			{
				LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
				return this;
			}

			Player.SetExclusiveMode(true);

			float targetVolInDb = AudioExtension.ToDecibel(othersVol);
			EffectParameter parameter = new EffectParameter()
			{
				Value = targetVolInDb,
				FadeTime = fadeTime,
				Type = EffectType.Volume
			};

			SoundManager.Instance.SetMainGroupTrackParameter(parameter, IsEndPlaying);
			return this;
		}

		IPlayerExclusive IPlayerExclusive.LowPassOthers(float freq ,float fadeTime)
        {
            if (!IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetExclusiveMode(true);

            EffectParameter parameter = new EffectParameter()
            {
                Value = freq,
                FadeTime = fadeTime,
                Type = EffectType.LowPass
            };

            SoundManager.Instance.SetMainGroupTrackParameter(parameter, IsEndPlaying);
            return this;
        }

        IPlayerExclusive IPlayerExclusive.HighPassOthers(float freq,float fadeTime)
        {
            if (!IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetExclusiveMode(true);

            EffectParameter parameter = new EffectParameter()
            {
                Value = freq,
                FadeTime = fadeTime,
                Type = EffectType.HighPass
            };

            SoundManager.Instance.SetMainGroupTrackParameter(parameter, IsEndPlaying);
            return this;
        }

        private bool IsValidFrequence(float freq)
        {
            if (freq < AudioConstant.MinFrequence || freq > AudioConstant.MaxFrequence)
            {
                LogWarning($"The given frequence should be in {AudioConstant.MinFrequence}Hz ~ {AudioConstant.MaxFrequence}Hz.");
                return false;
            }
            return true;
        }

        private bool IsEndPlaying()
		{
            if(Player != null)
			{
                return !Player.IsPlaying;
            }
            return true;
		}
    }
}