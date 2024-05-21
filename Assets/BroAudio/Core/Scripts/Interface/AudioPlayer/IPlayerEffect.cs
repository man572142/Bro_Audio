namespace Ami.BroAudio
{
    public interface IPlayerEffect : IVolumeSettable,IMusicDecoratable
    {
#if !UNITY_WEBGL
#if UNITY_2020_2_OR_NEWER
        internal IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        internal IPlayerEffect QuietOthers(float othersVol, Fading fading);
        internal IPlayerEffect LowPassOthers(float freq, float fadeTime);
        internal IPlayerEffect LowPassOthers(float freq, Fading fading);
        internal IPlayerEffect HighPassOthers(float freq, float fadeTime);
        internal IPlayerEffect HighPassOthers(float freq, Fading fading);
#else
        IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        IPlayerEffect QuietOthers(float othersVol, Fading fading);
        IPlayerEffect LowPassOthers(float freq, float fadeTime);
        IPlayerEffect LowPassOthers(float freq, Fading fading);
        IPlayerEffect HighPassOthers(float freq, float fadeTime);
        IPlayerEffect HighPassOthers(float freq, Fading fading);
#endif  
#endif
    }
}