namespace Ami.BroAudio.Runtime
{
    public interface IAudioBus
    {
        void UpdateVolume(bool forceUpdate = false);
    } 
}