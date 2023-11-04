using UnityEngine;

namespace Ami.BroAudio
{
    [System.Serializable]
    public struct TriggerParameter
	{
        public SoundContainer SoundContainer;
        public Transform Target;
		public float FloatValue;
		public Vector3 Position;
		public BroAudioType AudioType;
	}
}