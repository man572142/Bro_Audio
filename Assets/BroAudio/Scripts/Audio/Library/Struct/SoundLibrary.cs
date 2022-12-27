using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        [SerializeField] string Name;
        public int ID;
        public BroAudioClip[] Clips;
        public float Delay;

		#region Properties Setting
        public bool IsShowClipView;
		public MulticlipsPlayMode MulticlipsPlayMode;
        #endregion

        private BroAudioClip _clip;


        public string EnumName  => Name;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary), index, Clips, ID);
        }

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

    }
}