namespace Ami.BroAudio
{
    public interface IPlayerEffect : IVolumeSettable,IMusicDecoratable
    {
#if UNITY_2020_2_OR_NEWER
        internal IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        internal IPlayerEffect LowPassOthers(float freq, float fadeTime);
        internal IPlayerEffect HighPassOthers(float freq, float fadeTime);
#else
        IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        IPlayerEffect LowPassOthers(float freq, float fadeTime);
        IPlayerEffect HighPassOthers(float freq, float fadeTime);
#endif
    }
}