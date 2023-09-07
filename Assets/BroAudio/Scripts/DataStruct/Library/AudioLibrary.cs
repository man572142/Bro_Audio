using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public class AudioLibrary : IAudioLibrary, IAudioEntity
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;

        public BroAudioClip[] Clips;

        [field: SerializeField] public float Delay { get; set; }
        [field: SerializeField] public bool Loop { get; set; }
        [field: SerializeField] public bool SeamlessLoop { get; set; }
        [field: SerializeField] public float TransitionTime { get; set; }

        public BroAudioClip Clip => Clips.PickNewOne(MulticlipsPlayMode, ID);

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

        public static class NameOf
		{
            public static string IsShowClipPreview => nameof(AudioLibrary.IsShowClipPreview);
            public static string MulticlipsPlayMode => nameof(AudioLibrary.MulticlipsPlayMode);
            public static string SeamlessType => nameof(AudioLibrary.SeamlessTransitionType);
            public static string TransitionTempo => nameof(AudioLibrary.TransitionTempo);
        }

        [SerializeField] private bool IsShowClipPreview = false;
        [SerializeField] private SeamlessType SeamlessTransitionType = SeamlessType.ClipSetting;
        [SerializeField] private TempoTransition TransitionTempo = default;
#endif
    }
}