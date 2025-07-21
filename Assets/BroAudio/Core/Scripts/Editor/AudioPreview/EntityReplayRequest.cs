using System;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Editor
{
    public class EntityReplayRequest : ReplayRequest
    {
        private readonly AudioEntity _entity;
        private readonly Action<int> _onReplay;
        
        private int _clipIndex;
        private int _context;
        private float _masterVolume = AudioConstant.FullVolume;
        private float _pitch = AudioConstant.DefaultPitch;
        
        public override float MasterVolume => _masterVolume;
        public override float Pitch => _pitch;

        public EntityReplayRequest(AudioEntity entity, Action<int> onReplay) : base(null)
        {
            _entity = entity;
            _onReplay = onReplay;
            if (entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                _context = (int)PlaybackStage.Loop;
            }
        }

        public override bool CanReplay()
        {
            if (_entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                return _entity.Clips.Length > _context - 1;
            }
            return base.CanReplay();
        }
        
        public override AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip(_context, out _clipIndex);
            return base.GetAudioClipForScheduling();
        }

        public override void Start()
        {
            _masterVolume = _entity.GetMasterVolume();
            _pitch = _entity.GetPitch();
            _onReplay?.Invoke(_clipIndex);

            if (_entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                var nextStage = _context == (int)PlaybackStage.End ? (int)PlaybackStage.Start : _context + 1;
                _context = nextStage;
            }
        }
    }
}
