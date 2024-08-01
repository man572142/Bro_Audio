using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public class AudioEntity : IEntityIdentity, IAudioEntity
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public int ID { get; private set; }

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;

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

        public BroAudioClip PickNewClip() => Clips.PickNewOne(MulticlipsPlayMode, ID, out _);
        public BroAudioClip PickNewClip(out int index) => Clips.PickNewOne(MulticlipsPlayMode, ID, out index);
        public BroAudioClip PickNewClip(int velocity) => Clips.PickNewOne(MulticlipsPlayMode, ID, out _, velocity);

        public bool Validate()
        {
            return Utility.Validate(Name, Clips, ID);
        }

        public float GetMasterVolume()
        {
            return GetRandomValue(MasterVolume, RandomFlag.Volume);
        }

        public float GetPitch()
        {
            return GetRandomValue(Pitch, RandomFlag.Pitch);
        }

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

            float half = range * 0.5f;
            return baseValue + Random.Range(-half, half);
        }

        public void ResetShuffleInUseState()
        {
            Clips.ResetIsUse();
        }

#if UNITY_EDITOR
        public enum SeamlessType
        {
            ClipSetting,
            Time,
            Tempo
        }

        [System.Serializable]
        public struct TempoTransition
        {
            public float BPM;
            public int Beats;
        }

        public static class EditorPropertyName
		{
            public static string MulticlipsPlayMode => nameof(AudioEntity.MulticlipsPlayMode);
            public static string SeamlessType => nameof(AudioEntity.SeamlessTransitionType);
            public static string TransitionTempo => nameof(AudioEntity.TransitionTempo);
            public static string SnapToFullVolume => nameof(AudioEntity.SnapToFullVolume);
        }

        [SerializeField] private SeamlessType SeamlessTransitionType = SeamlessType.ClipSetting;
        [SerializeField] private TempoTransition TransitionTempo = default;
        [SerializeField] private bool SnapToFullVolume = false;
#endif
    }
}