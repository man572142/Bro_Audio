using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        [SerializeField] string _name;
        public AudioClip Clip;
        public Sound Sound;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float Delay;
        [Min(0f)] public float StartPosition;

        public string EnumName  => _name;

		public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary), index, Clip, StartPosition);
        }
    }
}