using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public partial class AudioEntity : IEntityIdentity, IAudioEntity
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public int ID { get; private set; }

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;
        [SerializeField] private PlaybackGroup _group;

        private PlaybackGroup _upperGroup;

        public BroAudioClip[] Clips;

        [field: SerializeField] public float MasterVolume { get; private set; }
        [field: SerializeField] public bool Loop { get; private set; }
        [field: SerializeField] public bool SeamlessLoop { get; private set; }
        [field: SerializeField] public float TransitionTime { get; private set; }
        [field: SerializeField] public SpatialSetting SpatialSetting { get; private set; }
        [field: SerializeField] public int Priority { get; private set; }
        [field: SerializeField] public float Pitch { get; private set; }
        [field: SerializeField] public float PitchRandomRange { get; private set; }
        [field: SerializeField] public float VolumeRandomRange { get; private set; }
        [field: SerializeField] public RandomFlag RandomFlags { get; private set; }
        public PlaybackGroup PlaybackGroup => _group ? _group : _upperGroup;

        public IBroAudioClip PickNewClip() => Clips.PickNewOne(MulticlipsPlayMode, ID, out _);
        public IBroAudioClip PickNewClip(out int index) => Clips.PickNewOne(MulticlipsPlayMode, ID, out index);
        public IBroAudioClip PickNewClip(int context) => Clips.PickNewOne(MulticlipsPlayMode, ID, out _, context);

        public bool Validate() => Utility.Validate(Name, Clips, ID);
        public MulticlipsPlayMode GetMulticlipsPlayMode() => MulticlipsPlayMode;
        public float GetMasterVolume() => GetRandomValue(MasterVolume, RandomFlag.Volume);
        public float GetPitch() => GetRandomValue(Pitch, RandomFlag.Pitch);

        public float GetRandomValue(float baseValue, RandomFlag flag)
        {
            if(!RandomFlags.Contains(flag))
            {
                return baseValue;
            }

            float range = flag switch 
            { 
                RandomFlag.Pitch => PitchRandomRange,
                RandomFlag.Volume => VolumeRandomRange,
                _ => throw new System.InvalidOperationException(),
            };

            return GetRandomValue(baseValue, range);
        }

        public static float GetRandomValue(float baseValue, float range)
        {
            float half = range * 0.5f;
            return baseValue + Random.Range(-half, half);
        }
        
        public bool HasLoop(out LoopType loopType, out float transitionTime)
        {
            loopType = LoopType.None;
            transitionTime = 0f;
            if (Loop)
            {
                loopType = LoopType.Loop;
            }
            else if (SeamlessLoop)
            {
                loopType = LoopType.SeamlessLoop;
                transitionTime = TransitionTime;
            }
            else if (MulticlipsPlayMode == MulticlipsPlayMode.Chained)
            {
                loopType = SoundManager.Instance.Setting.DefaultChainedPlayModeLoop;
                transitionTime = SoundManager.Instance.Setting.DefaultChainedPlayModeTransitionTime;
            }
            return loopType != LoopType.None;
        }

        public void ResetShuffleInUseState()
        {
            ShuffleClipStrategy.ResetIsUse(Clips);
        }

        public void LinkPlaybackGroup(PlaybackGroup upperGroup)
        {
            if (_group != null)
            {
                _group.SetParent(upperGroup);
            }
            else
            {
                _upperGroup = upperGroup;
            }
        }
        
        public override string ToString()
        {
            return Name;
        }

#if UNITY_EDITOR

        public static class EditorPropertyName
		{
            public static string MulticlipsPlayMode => nameof(AudioEntity.MulticlipsPlayMode);
            public static string PlaybackGroup => nameof(AudioEntity._group);
            public static string SeamlessType => nameof(AudioEntity.SeamlessTransitionType);
            public static string TransitionTempo => nameof(AudioEntity.TransitionTempo);
            public static string SnapToFullVolume => nameof(AudioEntity.SnapToFullVolume);
        }

        [SerializeField] private SeamlessType SeamlessTransitionType = SeamlessType.ClipSetting;
        [SerializeField] private TempoTransition TransitionTempo = default;
        [SerializeField] private bool SnapToFullVolume = false;

        public void ReassignID(int id)
        {
            ID = id;
        }
#endif
    }
}