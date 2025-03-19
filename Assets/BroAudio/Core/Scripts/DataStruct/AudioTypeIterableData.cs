using System;
using System.Collections.Generic;

namespace Ami.BroAudio.Runtime
{
    public struct PlaybackPrefSetter<TParameter> : IAudioTypeIterable
    {
        public BroAudioType TargetType;
        public IReadOnlyDictionary<BroAudioType, AudioTypePlaybackPreference> AudioTypePref;
        public Action<AudioTypePlaybackPreference, TParameter> OnModifyPref;
        public TParameter Parameter;

        public void OnEachAudioType(BroAudioType audioType)
        {
            if (TargetType.Contains(audioType) && AudioTypePref.TryGetValue(audioType, out var pref))
            {
                OnModifyPref.Invoke(pref, Parameter);
            }
        }
    }

    public struct PlaybackPrefInitializer : IAudioTypeIterable
    {
        public Dictionary<BroAudioType, AudioTypePlaybackPreference> AudioTypePref;

        public void OnEachAudioType(BroAudioType audioType)
        {
            AudioTypePref?.Add(audioType, new AudioTypePlaybackPreference());
        }
    }

    public struct OriginVolumeRecorder : IAudioTypeIterable
    {
        public BroAudioType TargetType;
        public Dictionary<BroAudioType, float> SystemOriginalVolumes;
        public void OnEachAudioType(BroAudioType audioType)
        {
            if (TargetType.Contains(audioType) && SoundManager.Instance.TryGetAudioTypePref(audioType, out var pref))
            {
                SystemOriginalVolumes[audioType] = pref.Volume;
            }
        }
    }
}