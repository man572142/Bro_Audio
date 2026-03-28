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
        private readonly Func<bool> _isReplayEnabled;

        private int _clipIndex;
        private int _context;
        private float _masterVolume = AudioConstant.FullVolume;
        private float _pitch = AudioConstant.DefaultPitch;

        public override float MasterVolume => _masterVolume;
        public override float Pitch => _pitch;

        public EntityReplayRequest(AudioEntity entity, Action<int> onReplay, Func<bool> isReplayEnabled = null) : base(null)
        {
            _entity = entity;
            _onReplay = onReplay;
            _isReplayEnabled = isReplayEnabled;
            if (entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                _context = (int)PlaybackStage.Loop;
            }
        }

        public override bool CanReplay()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                if (_context == (int)PlaybackStage.None)
                {
                    return false;
                }
                if (_context == (int)PlaybackStage.End && _entity.Clips.Length < (int)PlaybackStage.End)
                {
                    return false;
                }
                return true;
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

            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                _context = GetNextChainedContext();
            }
        }

        private int GetNextChainedContext()
        {
            if (_context == (int)PlaybackStage.End)
            {
                return (int)PlaybackStage.None;
            }
            if (_context == (int)PlaybackStage.Loop && (_isReplayEnabled?.Invoke() ?? false))
            {
                return (int)PlaybackStage.Loop;
            }
            return _context + 1;
        }
    }
}
