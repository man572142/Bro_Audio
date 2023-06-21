namespace MiProduction.BroAudio
{
    public interface IPlayerEffect : IVolumeSettable,IPlaybackControlGettable,IMusicDecoratable
    {
    	internal IPlayerEffect QuietOthers(float othersVol, float fadeTime);
        internal IPlayerEffect LowPassOthers(float freq, float fadeTime);
        internal IPlayerEffect HighPassOthers(float freq, float fadeTime);
    }
}