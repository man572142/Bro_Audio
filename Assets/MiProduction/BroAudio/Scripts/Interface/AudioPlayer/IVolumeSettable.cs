namespace MiProduction.BroAudio
{
    public interface IVolumeSettable
    {
        internal IAudioPlayer SetVolume(float vol, float fadeTime);
    }
}    
    
