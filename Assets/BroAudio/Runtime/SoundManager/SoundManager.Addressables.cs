#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION
using System;
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
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out _))
            {
                return IsLocalizationHandleLoaded(id);
            }
#endif
            return TryGetAddressableEntity(id, out var entity) && entity.IsLoaded();
        }

        public bool IsLoaded(SoundID id, int clipIndex)
        {
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out _))
            {
                return IsLocalizationHandleLoaded(id);
            }
#endif
            return TryGetAddressableEntity(id, out var entity) && entity.IsLoaded(clipIndex);
        }

        public AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id)
        {
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out var localizationEntity))
            {
                return LoadAllLocalizedAssetsAsync(id, localizationEntity);
            }
#endif
            if (TryGetAddressableEntity(id, out var entity))
            {
                var result = entity.LoadAssetsAsync();
                UpdateLoadedEntityLastPlayedTime(id);
                return result;
            }

            return default;
        }

        public AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id, Action<SoundID> onLoaded)
            => AttachLoadedCallback(LoadAllAssetsAsync(id), id, onLoaded);

        public AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex)
        {
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out var localizationEntity))
            {
                return LoadLocalizedAssetAsync(id, localizationEntity);
            }
#endif
            if (TryGetAddressableEntity(id, out var entity) && clipIndex >= 0 && clipIndex < entity.Clips.Length)
            {
                var clip = entity.Clips[clipIndex];
                var result = clip.LoadAssetAsync();
                UpdateLoadedEntityLastPlayedTime(id);
                return result;
            }
            return default;
        }

        public AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex, Action<SoundID> onLoaded)
            => AttachLoadedCallback(LoadAssetAsync(id, clipIndex), id, onLoaded);

        public void ReleaseAllAssets(SoundID id)
        {
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out _))
            {
                ReleaseLocalizationHandleInternal(id);
                return;
            }
#endif
            if (TryGetAddressableEntity(id, out var entity))
            {
                entity.ReleaseAllAssets();
            }
        }

        public void ReleaseAsset(SoundID id, int clipIndex)
        {
#if PACKAGE_LOCALIZATION
            if (TryGetLocalizationEntity(id, out _))
            {
                ReleaseLocalizationHandleInternal(id);
                return;
            }
#endif
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
                    Debug.LogError($"The entity {id.ToString().ToBold()} isn’t marked as addressable. Please check its settings.");
                }
                return entity.UseAddressables;
            }
            return false;
        }

        private static AsyncOperationHandle<T> AttachLoadedCallback<T>(
            AsyncOperationHandle<T> handle, SoundID id, Action<SoundID> onLoaded)
        {
            if (onLoaded == null || !handle.IsValid())
            {
                return handle;
            }

            handle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onLoaded(id);
                }
            };
            return handle;
        }
    }
}
#endif