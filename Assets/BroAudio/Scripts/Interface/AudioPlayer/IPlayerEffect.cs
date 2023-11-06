namespace Ami.BroAudio
{
    public interface IPlayerEffect : IVolumeSettable,IMusicDecoratable
    {
#if UNITY_2020_2_OR_NEWER
        internal IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        internal IPlayerEffect HighCutOthers(float freq, float fadeTime);
        internal IPlayerEffect LowCutOthers(float freq, float fadeTime);
#else
        IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        IPlayerEffect HighCutOthers(float freq, float fadeTime);
        IPlayerEffect LowCutOthers(float freq, float fadeTime);
#endif
    }
}