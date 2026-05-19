#if PACKAGE_LOCALIZATION
using System.Collections.Generic;
using Ami.BroAudio.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        private bool _isSubscribedToLocaleChanged;
        private Dictionary<SoundID, AsyncOperationHandle<AudioClip>> _preloadedLocalizationHandles;

        /// <summary>
        ///     Resolves the locale-correct AudioClip for the given Localization-mode entity, caching the
        ///     handle by SoundID so subsequent calls return the same handle and a paired
        ///     <see cref="SoundManager.ReleaseAsset(SoundID,int)"/> can release it.
        ///     Subscribes to <see cref="LocalizationSettings.SelectedLocaleChanged"/> on first cache add so
        ///     the cache stays in sync with the active locale.
        ///     Returns <c>default</c> and logs a warning if the table or entry reference is empty.
        /// </summary>
        private AsyncOperationHandle<AudioClip> LoadLocalizedAssetAsync(SoundID id, AudioEntity audioEntity)
        {
            if (audioEntity.LocalizationTable.ReferenceType == TableReference.Type.Empty ||
                audioEntity.LocalizationEntry.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogWarning(Utility.LogTitle +
                                 $"LocalizationTable or LocalizationEntry is not set on entity '{audioEntity.Name}'.");
                return default;
            }

            _preloadedLocalizationHandles ??= new Dictionary<SoundID, AsyncOperationHandle<AudioClip>>();

            if (_preloadedLocalizationHandles.TryGetValue(id, out AsyncOperationHandle<AudioClip> cached) && cached.IsValid())
            {
                return cached;
            }

            if (!_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = true;
            }

            AsyncOperationHandle<AudioClip> handle = LocalizationSettings.AssetDatabase
                .GetLocalizedAssetAsync<AudioClip>(audioEntity.LocalizationTable, audioEntity.LocalizationEntry);
            _preloadedLocalizationHandles[id] = handle;
            return handle;
        }

        /// <summary>
        ///     Wraps the single resolved localized clip into an <see cref="IList{AudioClip}"/> handle so the
        ///     return type matches the Addressables path of <see cref="LoadAllAssetsAsync"/>.
        /// </summary>
        private AsyncOperationHandle<IList<AudioClip>> LoadAllLocalizedAssetsAsync(SoundID id, AudioEntity audioEntity)
        {
            AsyncOperationHandle<AudioClip> clipHandle = LoadLocalizedAssetAsync(id, audioEntity);
            if (!clipHandle.IsValid())
            {
                return default;
            }

            return Addressables.ResourceManager.CreateChainOperation<IList<AudioClip>, AudioClip>(clipHandle, op =>
            {
                IList<AudioClip> result = new List<AudioClip> { op.Result };
                return Addressables.ResourceManager.CreateCompletedOperation(result, string.Empty);
            });
        }

        private void ReleaseLocalizationHandleInternal(SoundID id)
        {
            if (_preloadedLocalizationHandles == null)
            {
                return;
            }

            if (_preloadedLocalizationHandles.TryGetValue(id, out AsyncOperationHandle<AudioClip> handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                _preloadedLocalizationHandles.Remove(id);
            }
        }

        private void OnSelectedLocaleChanged(Locale newLocale)
        {
            if (_preloadedLocalizationHandles == null || _preloadedLocalizationHandles.Count == 0)
            {
                return;
            }

            List<SoundID> ids = new(_preloadedLocalizationHandles.Keys);
            foreach (AsyncOperationHandle<AudioClip> handle in _preloadedLocalizationHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _preloadedLocalizationHandles.Clear();

            foreach (SoundID id in ids)
            {
                if (TryGetEntity(id, out IAudioEntity entity) && entity is AudioEntity audioEntity
                    && audioEntity.PlayMode == MulticlipsPlayMode.Localization)
                {
                    LoadLocalizedAssetAsync(id, audioEntity);
                }
            }
        }

        private void ReleaseAllLocalizationPreloads()
        {
            if (_preloadedLocalizationHandles != null)
            {
                foreach (AsyncOperationHandle<AudioClip> handle in _preloadedLocalizationHandles.Values)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }

                _preloadedLocalizationHandles.Clear();
            }

            if (_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = false;
            }
        }
    }
}
#endif