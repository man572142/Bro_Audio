namespace Ami.BroAudio
{
    public interface IPlayableValidator
    {
        // TODO: Split them
        bool IsPlayable(SoundID id);
        void HandlePlayer(IAudioPlayer player);
    }
}