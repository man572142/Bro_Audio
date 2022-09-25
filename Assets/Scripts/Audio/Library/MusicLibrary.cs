using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [System.Serializable]
    public struct MusicLibrary : IAudioLibrary
    {
        [SerializeField] string _name;
        public AudioClip Clip;
        public Music Music;
        [Range(0f, 1f)] public float Volume;
        public float StartPosition;
        //[MinMaxSlider(0f,1f)] public Vector2 fade;
        [Min(0f)] public float FadeIn;
        [Min(0f)] public float FadeOut;
        [Min(0f)] public bool Loop;

        string IAudioLibrary.EnumName => _name;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary), index, Clip, StartPosition, FadeIn, FadeOut);
        }
    }
}