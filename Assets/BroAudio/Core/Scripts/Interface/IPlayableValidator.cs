namespace Ami.BroAudio
{
    public interface IPlayableValidator
    {
        bool IsPlayable(SoundID id);
        void OnGetPlayer(IAudioPlayer player);
    }
}