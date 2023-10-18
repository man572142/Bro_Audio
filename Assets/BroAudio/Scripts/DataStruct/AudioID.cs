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

		public AudioID(int iD) : this()
		{
			ID = iD;
		}

		public static implicit operator int(AudioID audioID) => audioID.ID;
	}
}