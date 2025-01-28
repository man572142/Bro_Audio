namespace Ami.BroAudio
{
    /// <summary>
    /// Determines whether a sound can be played. This is often used with overriding the PlaybackGroup.
    /// </summary>
    /// <seealso cref="PlaybackGroup"/>
    public interface IPlayableValidator
    {

        ///<inheritdoc cref="PlaybackGroup.IsPlayable(SoundID)"/>
        bool IsPlayable(SoundID id);

        ///<inheritdoc cref="PlaybackGroup.OnGetPlayer(IAudioPlayer)"/>
        void OnGetPlayer(IAudioPlayer player);
    }
}