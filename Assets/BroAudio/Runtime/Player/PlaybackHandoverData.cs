using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public struct PlaybackHandoverData
    {
        public SoundID ID;
        public PlaybackPreference Pref;
        public IBroAudioClip Clip;
        public EffectType PreviousTrackEffect;
        public float TrackVolume;
        public float Pitch;
    }
}