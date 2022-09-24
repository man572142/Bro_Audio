using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "BroAudio/Create Library/Music")]
	public class MusicLibraryAsset : AudioLibraryAsset<MusicLibrary>
	{

	}


    [System.Serializable]
    public struct MusicLibrary : IAudioLibrary
    {
        public string Name;
        public AudioClip Clip;
        public Music Music;
        [Range(0f, 1f)] public float Volume;
        public float StartPosition;
        //[MinMaxSlider(0f,1f)] public Vector2 fade;
        [Min(0f)] public float FadeIn;
        [Min(0f)] public float FadeOut;
        [Min(0f)] public bool Loop;

        public string GetName() => Name;

		public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary), index, Clip, StartPosition, FadeIn, FadeOut);
        }
    }
}