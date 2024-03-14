using UnityEngine;

namespace Ami.BroAudio
{
	[System.Serializable]
	public struct SoundID
	{
		public int ID;
#if UNITY_EDITOR
		[SerializeField] private ScriptableObject _sourceAsset;
#endif

		public SoundID(int id) : this()
		{
			ID = id;
		}

		public SoundID(BroAudioType audioType, int index) : this()
		{
            ID = audioType.GetInitialID() + index;
        }

		public static implicit operator int(SoundID soundID) => soundID.ID;
        public static implicit operator SoundID(int id) => new SoundID(id);

#if UNITY_EDITOR
		public static class NameOf
		{
			public static string SourceAsset => nameof(_sourceAsset);
		} 
#endif
	}
}