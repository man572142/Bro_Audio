using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public class SoundLibrary : AudioLibrary
    {
        public float Delay;

        protected override string DisplayName => nameof(SoundLibrary);
	}
}