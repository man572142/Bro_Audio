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
        private PlaybackStage _stage;
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
                _stage = PlaybackStage.Loop; // Start clip is played externally by PreviewRequest
            }
        }

        public override bool CanReplay()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                return ChainedPlaybackHelper.CanReplay(_stage, _entity.Clips.Length);
            }
            return base.CanReplay();
        }

        public override bool TryProceedToEnd()
        {
            if (_entity.PlayMode == MulticlipsPlayMode.Chained && _loopMode && !_proceedToEnd)
            {
                if (ChainedPlaybackHelper.CanHandoverToEnd(_stage, _entity.Clips.Length))
                {
                    _proceedToEnd = true;
                    _stage = ChainedPlaybackHelper.AdvanceToEnd();
                    GetAudioClipForScheduling();
                    _onReplay?.Invoke(_clipIndex);
                    return true;
                }
            }
            return false;
        }
        
        public override AudioClip GetAudioClipForScheduling()
        {
            Clip = _entity.PickNewClip((int)_stage, out _clipIndex);
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
                        _stage = (PlaybackStage)((int)_stage + 1);
                    }
                    // else: stay at Loop (no-op)
                }
                else
                {
                    _stage = (PlaybackStage)((int)_stage + 1);
                }
            }
        }
    }
}
