using UnityEngine;

namespace Ami.BroAudio
{
	[System.Serializable]
	public struct AudioID
	{
		public int ID;
#if UNITY_EDITOR
		public ScriptableObject SourceAsset;
#endif

		public AudioID(int id) : this()
		{
			ID = id;
		}

		public AudioID(BroAudioType audioType, int index) : this()
		{
            ID = audioType.GetInitialID() + index;
        }

		public static implicit operator int(AudioID audioID) => audioID.ID;
        public static implicit operator AudioID(int id) => new AudioID(id);
    }
}