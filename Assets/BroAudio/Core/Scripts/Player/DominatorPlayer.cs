using Ami.Extension;
using static UnityEngine.Debug;

namespace Ami.BroAudio.Runtime
{
	public class DominatorPlayer : AudioPlayerDecorator, IPlayerEffect
	{
        public DominatorPlayer(AudioPlayer instance) : base(instance)
        {
        }
#if !UNITY_WEBGL

        #region Quiet Others
        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime)
        {
            return this.QuietOthers(othersVol, new Fading(fadeTime, EffectType.Volume));
        }

        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, Fading fading)
        {
            if (othersVol <= 0f || othersVol > 1f)
            {
                LogWarning(Utility.LogTitle + "othersVol should be less than 1 and greater than 0.");
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

        private void SetAllEffectExceptDominator(Effect effect)
        {
            SoundManager.Instance.SetEffect(effect).While(PlayerIsPlaying);
            Instance?.SetEffect(EffectType.None,SetEffectMode.Override);
        }

        private bool PlayerIsPlaying() => IsActive;
#endif
    }
}