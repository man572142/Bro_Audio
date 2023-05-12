using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public abstract class AudioLibrary : IAudioEntity
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        public BroAudioClip[] Clips;

        #region Properties Setting
        public bool IsShowClipPreview;
        public MulticlipsPlayMode MulticlipsPlayMode;
        #endregion

        protected abstract string DisplayName { get; }

        private BroAudioClip _clip;
        public BroAudioClip Clip
        {
            get
            {
                if (_clip == null || _clip.IsNull())
                {
                    _clip = Clips.PickNewOne(MulticlipsPlayMode, ID);
                }
                return _clip;
            }
        }

        public bool Validate(int index)
        {
            return Utility.Validate(DisplayName, index, Clips, ID);
        }
    }
}