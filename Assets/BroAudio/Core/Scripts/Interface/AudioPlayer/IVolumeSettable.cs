namespace Ami.BroAudio
{
    public interface IVolumeSettable
    {
#if UNITY_2020_2_OR_NEWER
        internal IAudioPlayer SetVolume(float vol, float fadeTime);
#else
        IAudioPlayer SetVolume(float vol, float fadeTime);
#endif
    }
}    
    
