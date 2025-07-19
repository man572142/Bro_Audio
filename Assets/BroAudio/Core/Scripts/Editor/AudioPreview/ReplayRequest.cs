using System;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Editor
{
    public class ReplayRequest
    {
        private readonly AudioEntity _entity;
        private readonly Action<int> _onReplay;
        
        private int _clipIndex;
        private int _context;

        public IBroAudioClip Clip { get; private set; }
        public float MasterVolume { get; private set; } = AudioConstant.FullVolume;
        public float Pitch { get; private set; } = AudioConstant.DefaultPitch;
        public int StartSample => Clip.GetAudioClip().GetTimeSample(Clip.StartPosition);

        public ReplayRequest(AudioEntity entity, Action<int> onReplay)
        {
            _entity = entity;
            _onReplay = onReplay;
            if (entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                _context = (int)PlaybackStage.Loop;
            }
        }

        public AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip(_context, out _clipIndex);
            return Clip.GetAudioClip();
        }

        public void Start()
        {
            MasterVolume = _entity.GetMasterVolume();
            Pitch = _entity.GetPitch();
            _onReplay?.Invoke(_clipIndex);

            if (_entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                var nextStage = _context == (int)PlaybackStage.End ? (int)PlaybackStage.Start : _context + 1;
                _context = nextStage;
            }
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
