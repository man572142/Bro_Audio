namespace Ami.BroAudio.Runtime
{
    public struct PlaybackHandoverData
    {
        public SoundID ID;
        public PlaybackPreference Pref;
        public EffectType PreviousTrackEffect;
        public float TrackVolume;
        public float Pitch;
    }
}
