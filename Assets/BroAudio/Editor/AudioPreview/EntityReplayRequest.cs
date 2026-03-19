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
        private readonly bool _loopMode;

        private int _clipIndex;
        private int _context;
        private bool _proceedToEnd;
        private float _masterVolume = AudioConstant.FullVolume;
        private float _pitch = AudioConstant.DefaultPitch;

        public override float MasterVolume => _masterVolume;
        public override float Pitch => _pitch;

        public EntityReplayRequest(AudioEntity entity, Action<int> onReplay, bool loopMode = false) : base(null)
        {
            _entity = entity;
            _onReplay = onReplay;
            _loopMode = loopMode;
            if (entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                _context = (int)PlaybackStage.Loop;
            }
        }

        public override bool CanReplay()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                return _context <= (int)PlaybackStage.End && _entity.Clips.Length > _context - 1;
            }
            return base.CanReplay();
        }

        public override bool TryProceedToEnd()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained && _loopMode && !_proceedToEnd)
            {
                _proceedToEnd = true;
                return true;
            }
            return false;
        }
        
        public override AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip(_context, out _clipIndex);
            return base.GetAudioClipForScheduling();
        }

        public override double GetTransitionTime()
        {
            return IsSeamlessLoop(out float t) ? t : 0;
        }

        private bool IsSeamlessLoop(out float transitionTime)
        {
            transitionTime = 0f;
            if (_entity.SeamlessLoop)
            {
                transitionTime = _entity.TransitionTime;
                return true;
            }
            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                var setting = BroEditorUtility.RuntimeSetting;
                if (setting != null && setting.DefaultChainedPlayModeLoop == LoopType.SeamlessLoop)
                {
                    transitionTime = setting.DefaultChainedPlayModeTransitionTime;
                    return true;
                }
            }
            return false;
        }

        public override void Start()
        {
            _masterVolume = _entity.GetMasterVolume();
            _pitch = _entity.GetPitch();
            _onReplay?.Invoke(_clipIndex);

            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                if (_loopMode)
                {
                    if (_proceedToEnd)
                    {
                        _context++;
                    }
                    // else: stay at Loop (no-op)
                }
                else
                {
                    _context++;
                }
            }
        }
    }
}
