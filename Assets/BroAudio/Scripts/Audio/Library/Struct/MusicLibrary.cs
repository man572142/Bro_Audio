using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct MusicLibrary : IAudioLibrary
    {
        [SerializeField] string Name;
        public int ID;
        public BroAudioClip[] Clips;
		public bool Loop;

        #region Properties Setting
        public bool IsShowClipPreview;
        public MulticlipsPlayMode MulticlipsPlayMode;
        #endregion

        private BroAudioClip _clip;

        public string EnumName => Name;

        public BroAudioClip Clip
        {
            get
            {
                if (_clip.IsNull())
                {
                    _clip = Clips.PickNewOne(MulticlipsPlayMode, ID);
                }
                return _clip;
            }
        }

		public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary), index, Clips,ID);
        }
	}
}