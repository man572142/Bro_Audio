using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public abstract class AudioLibrary : IAudioLibrary
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        public BroAudioClip[] Clips;
        public BroAudioClip Clip => Clips.PickNewOne(MulticlipsPlayMode, ID);

        [SerializeField] private MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;

        public abstract BroAudioType PossibleFlags { get; }

        public bool Validate()
        {
            return Utility.Validate(Name.ToWhiteBold(), Clips, ID);
        }

#if UNITY_EDITOR
        [SerializeField] private bool IsShowClipPreview = false;
        
        public static string NameOf_IsShowClipPreview => nameof(IsShowClipPreview);
        public static string NameOf_MulticlipsPlayMode => nameof(MulticlipsPlayMode);
#endif
    }
}