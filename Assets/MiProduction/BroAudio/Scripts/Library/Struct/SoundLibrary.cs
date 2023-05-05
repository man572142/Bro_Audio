using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public struct SoundLibrary : IAudioEntity,IAudioLibraryEditorProperty
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public int ID { get; set; }

        public BroAudioClip[] Clips;
        public float Delay;

        #region Properties Setting
        [field: SerializeField] public bool IsShowClipPreview { get; set; }
        [field: SerializeField] public MulticlipsPlayMode MulticlipsPlayMode { get; set; }
        #endregion

        private BroAudioClip _clip;
        public BroAudioClip Clip
        {
			get
			{
                if(_clip == null || _clip.IsNull())
				{
                    _clip = Clips.PickNewOne(MulticlipsPlayMode, ID);
                }
                return _clip;
			}
        }

		public bool Validate(int index)
        {
            return Utility.Validate(nameof(SoundLibrary), index, Clips, ID);
        }
    }
}