public interface IAudioPlayer
{
    public IAudioPlayer StandsOut(float standoutRatio, float fadeTime);

    public IAudioPlayer LowPassOthers(float freq, float fadeTime);
}
