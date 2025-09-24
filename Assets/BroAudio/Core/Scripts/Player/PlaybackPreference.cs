using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Runtime.AudioPlayer;

namespace Ami.BroAudio.Runtime
{
    public struct PlaybackPreference
    {
        public readonly IAudioEntity Entity;
        private readonly Vector3? _position;
        private readonly Transform _followTarget;

        private int _contextValue;
        public double ScheduledStartTime { get; set; }
        public double ScheduledEndTime { get; set; }
        public float FadeIn { get; set; }
        public float FadeOut { get; set; }

        public PlaybackStage ChainedModeStage
        {
            get => (PlaybackStage)_contextValue;
            set => _contextValue = (int)value;
        }
        
        public Ease FadeInEase => IsLoop(LoopType.SeamlessLoop) ? SoundManager.SeamlessFadeIn : SoundManager.FadeInEase;
        public Ease FadeOutEase => IsLoop(LoopType.SeamlessLoop) ? SoundManager.SeamlessFadeOut : SoundManager.FadeOutEase;

        public Vector3 Position
        {
            get
            {
                if (!_position.HasValue)
                {
                    return _followTarget != null ? _followTarget.position : Utility.GloballyPlayedPosition;
                }
                return _position.Value;
            }
        }

        public PlaybackPreference(IAudioEntity entity, Vector3 position) : this(entity)
        {
            _position = position;
        }

        public PlaybackPreference(IAudioEntity entity, Transform followTarget) : this(entity)
        {
            _followTarget = followTarget;
        }

        public PlaybackPreference(IAudioEntity entity)
        {
            Entity = entity;
            FadeIn = UseEntitySetting;
            FadeOut = UseEntitySetting;
            ScheduledStartTime = 0;
            ScheduledEndTime = 0;
            _position = Utility.GloballyPlayedPosition;
            _followTarget = null;
            _contextValue = GetContextValue(entity);
        }

        public IBroAudioClip PickNewClip()
        {
            return Entity.PickNewClip(_contextValue);
        }

        public void ResetFading()
        {
            FadeIn = UseEntitySetting;
            FadeOut = UseEntitySetting;
        }

        public void ApplySeamlessFade()
        {
            if (Entity.HasLoop(out var loopType, out var transitionTime) && loopType == LoopType.SeamlessLoop)
            {
                FadeIn = transitionTime;
                FadeOut = transitionTime;
            }
        }

        public void SetVelocity(int velocity)
        {
            if (Entity.PlayMode != MulticlipsPlayMode.Velocity)
            {
                Debug.LogError($"Cannot set velocity on [{Entity}] because it's not using VelocityPlayMode. (current : {Entity.PlayMode})");
                return;
            }
            _contextValue = velocity;
        }

        public bool HasFollowTarget(out Transform target)
        {
            target = _followTarget;
            return target != null;
        }

        public bool HasSpecifiedPosition(out Vector3 position)
        {
            position = _position.GetValueOrDefault();
            return _position.HasValue && !Utility.IsPlayedGlobally(position);
        }
        
        public bool CanHandoverToLoop()
        {
            if (IsChainedMode() && (ChainedModeStage == PlaybackStage.End || ChainedModeStage == PlaybackStage.None))
            {
                return false;
            }
            return IsLoop(LoopType.SeamlessLoop) || 
                   (IsChainedMode() && ChainedModeStage == PlaybackStage.Start); // normal loop uses the same audio player to loop
        }

        public bool CanHandoverToEnd()
        {
            if (!IsChainedMode())
            {
                return false;
            }

            if (ChainedModeStage == PlaybackStage.Loop && Entity is AudioEntity entity && 
                entity.Clips.Length < (int)PlaybackStage.End)
            {
                return false;
            }
            
            return ChainedModeStage != PlaybackStage.End && ChainedModeStage != PlaybackStage.None;
        }

        public bool IsChainedMode()
        {
            return Entity.PlayMode == MulticlipsPlayMode.Chained;
        }
        
        public bool IsLoop(LoopType targetType)
        {
            return Entity.HasLoop(out var loopType, out _) && loopType == targetType;
        }

        private static int GetContextValue(IAudioEntity entity) => entity.PlayMode switch
        {
            MulticlipsPlayMode.Chained => (int)PlaybackStage.Start,
            _ => 0,
        };
    }
}