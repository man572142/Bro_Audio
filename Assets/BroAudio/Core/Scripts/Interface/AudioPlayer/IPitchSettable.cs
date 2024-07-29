namespace Ami.BroAudio
{
    public interface IPitchSettable
    {
        internal IAudioPlayer SetPitch(float pitch, float fadeTime);
    }
}