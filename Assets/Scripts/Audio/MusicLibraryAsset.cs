using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "BroAudio/Create Library/Music", order = 1)]
	public class MusicLibraryAsset : ScriptableObject
	{
		public MusicLibrary[] Libraries;
		public Dictionary<Music, MusicLibrary> MusicBank = new Dictionary<Music, MusicLibrary>();
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

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary), index, Clip, StartPosition, FadeIn, FadeOut);
        }
    }
}