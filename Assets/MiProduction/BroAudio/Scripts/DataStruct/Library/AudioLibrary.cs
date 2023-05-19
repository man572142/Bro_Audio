using UnityEngine;


namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public abstract class AudioLibrary : IAudioLibrary
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        public BroAudioClip[] Clips;

        #region Properties Setting
        public bool IsShowClipPreview;
        public MulticlipsPlayMode MulticlipsPlayMode;
        #endregion

        protected abstract string DisplayName { get; }

        public BroAudioClip Clip => Clips.PickNewOne(MulticlipsPlayMode, ID);

        public bool Validate(int index)
        {
            return Utility.Validate(DisplayName, index, Clips, ID);
        }
    }
}