using System;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Runtime;
using System.Collections;
using Ami.BroAudio.Data;

#if PACKAGE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Ami.BroAudio
{
    [Serializable]
    public struct SoundID : IEquatable<SoundID>, IComparable<SoundID>, IEqualityComparer<SoundID>, IComparer<SoundID>
#if !UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
        [SerializeField]
        private AudioEntity _entity;

        public AudioEntity Entity
        {
            get
            {
                if (_entity == null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    _fixLegacyId();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                return _entity;
            }
        }

        [System.Obsolete("Raw entities are now used", true)]
        [SerializeField]
        private int ID;

        [System.Obsolete("Raw entities are now used")]
        private void _fixLegacyId()
        {
            if (_entity != null)
            {
                //Debug.LogError($"ID {ID} is already set to {_entity.Name}");
                //ID = 0;
                return;
            }

            if (ID == 0 || ID == -1)
            {
                return;
            }

            if (!BroAudio.TryConvertIdToEntity(ID, out _entity))
            {
                Debug.LogError($"Could not find entity with ID {ID} to convert SoundID to entity with");
                _entity = null;
                return;
            }

            //ID = 0;
        }

#if !UNITY_EDITOR
#pragma warning disable CS0618 // Type or member is obsolete
        void ISerializationCallbackReceiver.OnBeforeSerialize () => _fixLegacyId();
        void ISerializationCallbackReceiver.OnAfterDeserialize () => _fixLegacyId();
#pragma warning restore CS0618 // Type or member is obsolete
#endif

#if UNITY_EDITOR
        [field: SerializeField] public GameObject DebugObject { get; private set; }
#endif

        public SoundID(AudioEntity entity) : this()
        {
            _entity = entity;
        }

        public static SoundID Invalid => new SoundID();

        public override string ToString() => Entity != null ? Entity.Name : "not set";
        public override bool Equals(object obj) => obj is SoundID soundID && Equals(soundID);
        public override int GetHashCode() => Entity != null ? Entity.GetHashCode() : 0;
        public bool Equals(SoundID other) => other.Entity == Entity;
        public bool Equals(SoundID x, SoundID y) => x.Entity == y.Entity;
        public int GetHashCode(SoundID obj) => obj.Entity != null ? obj.Entity.GetHashCode() : 0;
        public int CompareTo(SoundID other) => Entity != null && other.Entity != null ? StringComparer.OrdinalIgnoreCase.Compare(Entity.Name, other.Entity.Name) : 0;
        public int Compare(SoundID x, SoundID y) => x.Entity != null && y.Entity != null ? StringComparer.OrdinalIgnoreCase.Compare(x.Entity.Name, y.Entity.Name) : 0;

        [System.Obsolete("legacy upgrade only", true)]
        public static void __setLegacyId(ref SoundID soundId, int id)
        {
            soundId.ID = id;
        }

#if UNITY_EDITOR
        public static class NameOf
        {
            public const string Entity = nameof(_entity);

            [System.Obsolete("Raw entities are now used")]
            public const string ID = nameof(ID);
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
            return id.Entity != null ? id.Entity.AudioType : BroAudioType.None;
        }

        /// <summary>
        /// Gets the name of the entity
        /// </summary>
        public static string ToName(this SoundID id)
        {
            return id.ToString();
        }

        /// <summary>
        /// Checks if this ID is available in the SoundManager at runtime
        /// </summary>
        public static bool IsValid(this SoundID id)
        {
            return id.Entity != null;
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

        /// <inheritdoc cref="BroAudio.Play(SoundID, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, playableValidator);

        /// <inheritdoc cref="BroAudio.Play(SoundID, float, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, float fadeIn, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, fadeIn, playableValidator);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Vector3, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, Vector3 position, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, position, playableValidator);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Vector3, float, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, Vector3 position, float fadeIn, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, position, fadeIn, playableValidator);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Transform, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, Transform followTarget, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, followTarget, playableValidator);

        /// <inheritdoc cref="BroAudio.Play(SoundID, Transform, float, IPlayableValidator)"/>
        public static IAudioPlayer Play(this SoundID id, Transform followTarget, float fadeIn, IPlayableValidator playableValidator = null) 
            => BroAudio.Play(id, followTarget, fadeIn, playableValidator);

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