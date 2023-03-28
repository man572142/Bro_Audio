
namespace MiProduction.BroAudio.Asset.Core
{
    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        public string Name;
        public int ID;
        public BroAudioClip[] Clips;
        public float Delay;

		#region Properties Setting
        public bool IsShowClipPreview;
		public MulticlipsPlayMode MulticlipsPlayMode;
        #endregion

        private BroAudioClip _clip;

        public string EnumName  => Name;


        public BroAudioClip Clip
        {
			get
			{
                if(_clip.IsNull())
				{
                    _clip = Clips.PickNewOne(MulticlipsPlayMode, ID);
                }
                return _clip;
			}
        }

		int IAudioLibrary.ID => ID;

		public bool Validate(int index)
        {
            return Utility.Validate(nameof(SoundLibrary), index, Clips, ID);
        }
    }
}