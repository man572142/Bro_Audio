namespace MiProduction.BroAudio
{
    public interface IPlayerExclusive : IVolumeSettable,IPlaybackControlGettable,IMusicDecoratable
    {
    	internal IPlayerExclusive DuckOthers(float othersVol, float fadeTime);
        internal IPlayerExclusive LowPassOthers(float freq, float fadeTime);
        internal IPlayerExclusive HighPassOthers(float freq, float fadeTime);
    }
}