using UnityEngine;

namespace Ami.BroAudio
{
    [System.Serializable]
    public struct TriggerParameter
	{
        public SoundSource SoundSource;
        public Transform Target;
		public Collider Collider;
        [Min(0f)]
        public float FadeTime;
        public float Volume;
		public Vector3 Position;
		public BroAudioType AudioType;
        public bool OnlyTriggerOnce;
	}

	public enum TriggerParameterType
	{
        Source,
		Follow,
        Collider,
        FadeTime,
        Volume,
        Position,
        AudioType,
        OnlyTriggerOnce,
    }
}