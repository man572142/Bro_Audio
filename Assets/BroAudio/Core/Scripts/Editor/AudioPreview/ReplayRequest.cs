using System;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public class ReplayRequest
    {
        private readonly AudioEntity _entity;
        private readonly Action<int> _onReplayWithClipIndex;
        
        private int _clipIndex;

        public IBroAudioClip Clip { get; private set; }
        public float MasterVolume { get; private set; } = AudioConstant.FullVolume;
        public float Pitch { get; private set; } = AudioConstant.DefaultPitch;
        public int StartSample => Clip.GetAudioClip().GetTimeSample(Clip.StartPosition);

        public ReplayRequest(AudioEntity entity, Action<int> onReplayWithClipIndex)
        {
            _entity = entity;
            _onReplayWithClipIndex = onReplayWithClipIndex;
        }

        public AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip(out _clipIndex);
            return Clip.GetAudioClip();
        }

        public void Start()
        {
            MasterVolume = _entity.GetMasterVolume();
            Pitch = _entity.GetPitch();
            _onReplayWithClipIndex?.Invoke(_clipIndex);
        }

        public double GetDuration()
        {
            if (Clip == null)
            {
                return 0;
            }

            var length = Clip.GetAudioClip().GetPreciseLength();
            return (length - Clip.StartPosition - Clip.EndPosition) / Pitch;
        }
    }
}
