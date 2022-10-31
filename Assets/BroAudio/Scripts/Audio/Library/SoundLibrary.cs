using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        [SerializeField] string Name;
        public int ID;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume;
        public float Delay;
        public float StartPosition;
        public float EndPosition;
        public float FadeIn;
        public float FadeOut;

        public string EnumName  => Name;

		public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary), index, Clip, StartPosition);
        }
    }
}