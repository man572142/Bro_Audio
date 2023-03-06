public interface IAudioPlayer
{
    public IAudioPlayer SetVolume(float vol, float fadeTime = 1f);

    public IAudioPlayer StandsOut(float standoutRatio, float fadeTime);

    public IAudioPlayer LowPassOthers(float freq, float fadeTime);


}
