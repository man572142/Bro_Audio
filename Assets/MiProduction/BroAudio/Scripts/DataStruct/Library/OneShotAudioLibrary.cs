using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public class OneShotAudioLibrary : AudioLibrary
    {
        public float Delay;

		public override BroAudioType PossibleFlags => BroAudioType.UI | BroAudioType.SFX | BroAudioType.VoiceOver;
	}
}