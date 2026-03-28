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
        private PlaybackStage _context;
        private float _masterVolume = AudioConstant.FullVolume;
        private float _pitch = AudioConstant.DefaultPitch;
        private readonly float _crossfadeTime;

        public override float MasterVolume => _masterVolume;
        public override float Pitch => _pitch;
        public override float CrossfadeTime => _crossfadeTime;

        public EntityReplayRequest(AudioEntity entity, Action<int> onReplay, Func<bool> isReplayEnabled = null) : base(null)
        {
            _entity = entity;
            _onReplay = onReplay;
            _isReplayEnabled = isReplayEnabled;
            if (entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                _context = PlaybackStage.Loop;
            }

            var runtimeSetting = BroEditorUtility.RuntimeSetting;
            if (entity.HasLoop(out var loopType, out var transitionTime,
                runtimeSetting.DefaultChainedPlayModeLoop, runtimeSetting.DefaultChainedPlayModeTransitionTime)
                && loopType == LoopType.SeamlessLoop)
            {
                _crossfadeTime = transitionTime;
            }
        }

        public override bool CanReplay()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                if (_context == PlaybackStage.None)
                {
                    return false;
                }
                if (_context == PlaybackStage.End && _entity.Clips.Length < (int)PlaybackStage.End)
                {
                    return false;
                }
                return true;
            }
            return base.CanReplay();
        }

        public override AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip((int)_context, out _clipIndex);
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

        private PlaybackStage GetNextChainedContext()
        {
            if (_context == PlaybackStage.End)
            {
                return PlaybackStage.None;
            }
            if (_context == PlaybackStage.Loop && (_isReplayEnabled?.Invoke() ?? false))
            {
                return PlaybackStage.Loop;
            }
            return PlaybackStage.End;
        }
    }
}
