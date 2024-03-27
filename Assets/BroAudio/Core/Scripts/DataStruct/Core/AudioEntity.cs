using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public class AudioEntity : IEntityIdentity, IAudioEntity
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;

        public BroAudioClip[] Clips;

        [field: SerializeField] public float MasterVolume { get; set; }
        [field: SerializeField] public bool Loop { get; set; }
        [field: SerializeField] public bool SeamlessLoop { get; set; }
        [field: SerializeField] public float TransitionTime { get; set; }
        [field: SerializeField] public SpatialSetting SpatialSetting { get; set; }
        [field: SerializeField] public int Priority { get; set; }
        [field: SerializeField] public float Pitch { get; set; }
        [field: SerializeField] public float PitchRandomRange { get; set; }
        [field: SerializeField] public float VolumeRandomRange { get; set; }
        [field: SerializeField] public RandomFlags RandomFlags { get; set; }

        public BroAudioClip PickNewClip() => Clips.PickNewOne(MulticlipsPlayMode, ID);

        public bool Validate()
        {
            return Utility.Validate(Name.ToWhiteBold(), Clips, ID);
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