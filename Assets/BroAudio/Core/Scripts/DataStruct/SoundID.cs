using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
	[Serializable]
	public struct SoundID : IEquatable<SoundID>, IComparable<SoundID>, IEqualityComparer<SoundID>, IComparer<SoundID>
	{
		public int ID;
#if UNITY_EDITOR
		[SerializeField] private ScriptableObject _sourceAsset;
#endif
		public SoundID(int id) : this()
		{
			ID = id;
		}

        public override string ToString() => ID.ToString();
        public override bool Equals(object obj) => obj is SoundID soundID && Equals(soundID);
        public override int GetHashCode() => ID;
        public bool Equals(SoundID other) => other.ID == ID;
        public bool Equals(SoundID x, SoundID y) => x.ID == y.ID;
		public int GetHashCode(SoundID obj) => obj.ID;
        public int CompareTo(SoundID other) => other.ID.CompareTo(ID);
        public int Compare(SoundID x, SoundID y) => x.ID.CompareTo(y.ID);

        public static implicit operator int(SoundID soundID) => soundID.ID;
        public static implicit operator SoundID(int id) => new SoundID(id);

#if UNITY_EDITOR
		public static class NameOf
		{
			public static string SourceAsset => nameof(_sourceAsset);
		} 
#endif
	}

	public static class SoundIDExtension
	{
		public static BroAudioType ToAudioType(this SoundID id)
		{
			return Utility.GetAudioType(id);
		}

		public static string ToName(this SoundID id)
		{
			return Runtime.SoundManager.Instance.GetNameByID(id);
		}
	}
}