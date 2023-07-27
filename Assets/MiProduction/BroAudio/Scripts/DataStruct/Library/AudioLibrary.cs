using UnityEngine;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public abstract class AudioLibrary : IAudioLibrary
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        public BroAudioClip[] Clips;
        public BroAudioClip Clip => Clips.PickNewOne(MulticlipsPlayMode, ID);

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode;

        public abstract BroAudioType PossibleFlags { get; }

        public bool Validate(int index)
        {
            return Utility.Validate(Name.ToWhiteBold(), index, Clips, ID);
        }

#if UNITY_EDITOR
        [SerializeField] private bool IsShowClipPreview;
        
        public static string NameOf_IsShowClipPreview => nameof(IsShowClipPreview);
        public static string NameOf_MulticlipsPlayMode => nameof(MulticlipsPlayMode);
#endif
    }
}