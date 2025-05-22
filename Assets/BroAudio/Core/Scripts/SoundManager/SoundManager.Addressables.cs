#if PACKAGE_ADDRESSABLES
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using Ami.BroAudio.Data;
using System.Collections;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        public IEnumerable GetAddressableKeys(SoundID id)
        {
            if (TryGetAddressableEntity(id, out var entity))
            {
                return entity.GetAllAddressableKeys();
            }
            return null;
        }

        public object GetAddressableKey(SoundID id, int index)
        {
            if (TryGetAddressableEntity(id, out var entity))
            {
                return entity.GetAddressableKey(index);
            }
            return null;
        }

        public bool IsLoaded(SoundID id)
        {
            return TryGetAddressableEntity(id, out var entity) && entity.IsLoaded();
        }

        public bool IsLoaded(SoundID id, int clipIndex)
        {
            return TryGetAddressableEntity(id, out var entity) && entity.IsLoaded(clipIndex);
        }

        public AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id)
        {
            if (TryGetAddressableEntity(id, out var entity))
            {
                return entity.LoadAssetsAsync();
            }
            return default;
        }

        public AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex)
        {
            if (TryGetAddressableEntity(id, out var entity) && clipIndex >= 0 && clipIndex < entity.Clips.Length)
            {
                var clip = entity.Clips[clipIndex];
                return clip.LoadAssetAsync();
            }
            return default;
        }

        public void ReleaseAllAssets(SoundID id)
        {
            if (TryGetAddressableEntity(id, out var entity))
            {
                entity.ReleaseAllAssets();
            }
        }

        public void ReleaseAsset(SoundID id, int clipIndex)
        {
            if (TryGetAddressableEntity(id, out var entity) && clipIndex >= 0 && clipIndex < entity.Clips.Length)
            {
                entity.Clips[clipIndex].ReleaseAsset();
            }
        }

        private bool TryGetAddressableEntity(SoundID id, out AudioEntity entity)
        {
            entity = null;
            if(TryGetEntity(id, out var e))
            {
                entity = e as AudioEntity;
                if(!entity.UseAddressables)
                {
                    Debug.LogError($"The entity {id.ToName().ToBold()} isn’t marked as addressable. Please check its settings.");
                }
                return entity.UseAddressables;
            }
            return false;
        }
    }
} 
#endif