using Ami.BroAudio.Data;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    public struct PlaybackHandoverData
    {
        public SoundID ID;
        public PlaybackPreference Pref;
        public IBroAudioClip Clip;
        public EffectType TrackEffect;
        public float TrackVolume;
        public float TrackVolumeCurrent;
        public float TrackVolumeRemaining;
        public Ease TrackVolumeEase;
        public float Pitch;
    }
}
