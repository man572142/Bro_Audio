using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct MusicLibrary : IAudioLibrary
    {
        [SerializeField] string Name;
        public AudioClip Clip;
        public Music Music;
        [Range(0f, 1f)] public float Volume;
        public float StartPosition;
        public float EndPosition;
        //[MinMaxSlider(0f,1f)] public Vector2 fade;
        public float FadeIn;
        public float FadeOut;
        public bool Loop;

        string IAudioLibrary.EnumName => Name;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary), index, Clip, StartPosition, FadeIn, FadeOut);
        }
    }
}