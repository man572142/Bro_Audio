using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
	public class RandomSoundLibraryAsset : AudioLibraryAsset<RandomSoundLibrary>
	{

	}

    [System.Serializable]
    public struct RandomSoundLibrary : IAudioLibrary
    {
        public string Name;
        public AudioClip[] Clips;
        public Sound Sound;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float StartPosition;

        public string GetName() => Name;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(RandomSoundLibrary), index, Clips, StartPosition);
        }
    }
}
