using System;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Runtime;
using System.Collections;

#if PACKAGE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

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
            return SoundManager.Instance.GetNameByID(id);
        }

        /// <summary>
        /// Checks if this ID is available in the SoundManager at runtime
        /// </summary>
        public static bool IsValid(this SoundID id)
        {
            return id > 0 && SoundManager.Instance.IsIdInBank(id);
        }

        /// <summary>
        /// Gets the AudioClip based on the PlayMode set in the LibraryManager.
        /// </summary>
        public static AudioClip GetAudioClip(this SoundID id)
        {
            return SoundManager.Instance.GetAudioClip(id);
        }

        /// <summary>
        /// Gets the AudioClip based on the PlayMode and velocity set in the LibraryManager.
        /// </summary>
        public static AudioClip GetAudioClip(this SoundID id, int velocity)
        {
            return SoundManager.Instance.GetAudioClip(id, velocity);
        }

        public static AudioClip GetAudioClip(this SoundID id, PlaybackStage chainedModeStage)
        {
            return SoundManager.Instance.GetAudioClip(id, chainedModeStage);
        }
        
        /// <inheritdoc cref="BroAudio.HasAnyPlayingInstances(SoundID)"/>
        public static bool HasAnyPlayingInstances(this SoundID id)
        {
            return SoundManager.Instance.HasAnyPlayingInstances(id);
        }

        /// <inheritdoc cref="BroAudio.Play(SoundID)"/>
        public static IAudioPlayer Play(this SoundID id, PlaybackGroup overrideGroup = null) 
            => BroAudio.Play(id, overrideGroup);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Vector3)"/>
        public static IAudioPlayer Play(this SoundID id, Vector3 position, PlaybackGroup overrideGroup = null) 
            => BroAudio.Play(id, position, overrideGroup);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Transform)"/>
        public static IAudioPlayer Play(this SoundID id, Transform followTarget, PlaybackGroup overrideGroup = null) 
            => BroAudio.Play(id, followTarget, overrideGroup);

#if PACKAGE_ADDRESSABLES
        ///<inheritdoc cref="BroAudio.LoadAllAssetsAsync(SoundID)"/>
        public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(this SoundID id)
            => SoundManager.Instance.LoadAllAssetsAsync(id);

        ///<inheritdoc cref="BroAudio.LoadAssetAsync(SoundID)"/>
        public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id)
            => LoadAssetAsync(id, 0);

        ///<inheritdoc cref="BroAudio.LoadAssetAsync(SoundID,int)"/>
        public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id, int clipIndex)
            => SoundManager.Instance.LoadAssetAsync(id, clipIndex);

        ///<inheritdoc cref="BroAudio.ReleaseAllAssets(SoundID)"/>
        public static void ReleaseAllAssets(this SoundID id)
            => SoundManager.Instance.ReleaseAllAssets(id);

        ///<inheritdoc cref="BroAudio.ReleaseAsset(SoundID)"/>
        public static void ReleaseAsset(this SoundID id)
            => ReleaseAsset(id, 0);

        ///<inheritdoc cref="BroAudio.ReleaseAsset(SoundID, int)"/>
        public static void ReleaseAsset(this SoundID id, int clipIndex)
            => SoundManager.Instance.ReleaseAsset(id, clipIndex);

        public static IEnumerable GetAllAddressablesKeys(this SoundID id)
            => SoundManager.Instance.GetAddressableKeys(id);

        public static object GetAddressablesKey(this SoundID id)
            => GetAddressablesKey(id, 0);

        public static object GetAddressablesKey(this SoundID id, int index)
            => SoundManager.Instance.GetAddressableKey(id, index);
#endif
    }
}