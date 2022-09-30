using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        [SerializeField] string Name;
        public Sound Sound;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float Delay;
        [Min(0f)] public float StartPosition;

        public string EnumName  => Name;

		public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary), index, Clip, StartPosition);
        }
    }
}