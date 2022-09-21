using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	[CreateAssetMenu(fileName = "SoundLibrary", menuName = "BroAudio/Create Library/Sound", order = 0)]
	public class SoundLibraryAsset : ScriptableObject
	{
		public SoundLibrary[] Libraries;
	}


    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        public string Name;
        public AudioClip Clip;
        public Sound Sound;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float Delay;
        [Min(0f)] public float StartPosition;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary), index, Clip, StartPosition);
        }
    }
}