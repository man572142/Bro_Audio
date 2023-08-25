namespace Ami.BroAudio
{
    public interface IPlaybackControlGettable
    {
#if UNITY_2020_2_OR_NEWER
        internal IPlaybackControllable GetPlaybackControl();
#else
        IPlaybackControllable GetPlaybackControl();
#endif
    }
}