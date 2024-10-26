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
        [field: SerializeField] public GameObject DebugObject { get; private set; }
#endif
		public SoundID(int id) : this()
		{
			ID = id;
		}

		public static SoundID Invalid => new SoundID(-1);

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

        public static bool TryGetAsset(SoundID soundID, out Data.AudioAsset asset)
        {
            asset = soundID._sourceAsset as Data.AudioAsset;
            return asset;
        }
#endif
    }

	public static class SoundIDExtension
	{
        /// <summary>
        /// Gets the AudioType of the entity
        /// </summary>
        public static BroAudioType ToAudioType(this SoundID id)
		{
			return Utility.GetAudioType(id);
		}

        /// <summary>
        /// Gets the name of the entity
        /// </summary>
		public static string ToName(this SoundID id)
		{
			return Runtime.SoundManager.Instance.GetNameByID(id);
		}

        /// <summary>
        /// Checks if this ID is available in the SoundManager at runtime
        /// </summary>
		public static bool IsValid(this SoundID id)
		{
			return id > 0 && Runtime.SoundManager.Instance.IsIdInBank(id);
		}

        /// <summary>
        /// Gets the AudioClip based on the PlayMode set in the LibraryManager.
        /// </summary>
        public static AudioClip GetAudioClip(this SoundID id)
        {
            return Runtime.SoundManager.Instance.GetAudioClip(id);
        }

        /// <summary>
        /// Gets the AudioClip based on the PlayMode and velocity set in the LibraryManager.
        /// </summary>
        public static AudioClip GetAudioClip(this SoundID id, int velocity)
        {
            return Runtime.SoundManager.Instance.GetAudioClip(id, velocity);
        }

        /// <inheritdoc cref="BroAudio.Play(SoundID)"/>
        public static IAudioPlayer Play(this SoundID id) => BroAudio.Play(id);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Vector3)"/>
        public static IAudioPlayer Play(this SoundID id, Vector3 position) => BroAudio.Play(id, position);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Transform)"/>
        public static IAudioPlayer Play(this SoundID id, Transform followTarget) => BroAudio.Play(id, followTarget);
    }
}