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
        ///     Preloads the AudioClip for the given entity's current locale into the Addressables cache.
        ///     After this call, <c>WaitForCompletion()</c> during playback will return instantly.
        ///     Call <see cref="ReleaseLocalizationPreload" /> when the entity is no longer needed.
        /// </summary>
        public AsyncOperationHandle<AudioClip> PreloadLocalizationAssets(SoundID id)
        {
            if (!TryGetEntity(id, out IAudioEntity entity))
            {
                return default;
            }

            var audioEntity = entity as AudioEntity;
            if (audioEntity.PlayMode != MulticlipsPlayMode.Localization)
            {
                Debug.LogWarning(Utility.LogTitle +
                                 $"[{nameof(PreloadLocalizationAssets)}] Entity '{audioEntity.Name}' is not in Localization mode.");
                return default;
            }

            AsyncOperationHandle<AudioClip> handle = LoadLocalizedAssetAsync(id, audioEntity);
            if (!handle.IsValid())
            {
                return default;
            }

            _preloadedLocalizationHandles ??= new Dictionary<SoundID, AsyncOperationHandle<AudioClip>>();

            // Release any existing handle for this id before storing the new one
            ReleaseLocalizationHandleInternal(id);

            if (!_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = true;
            }

            _preloadedLocalizationHandles[id] = handle;
            return handle;
        }

        /// <summary>
        ///     Resolves the locale-correct AudioClip for the given Localization-mode entity.
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

            return LocalizationSettings.AssetDatabase
                .GetLocalizedAssetAsync<AudioClip>(audioEntity.LocalizationTable, audioEntity.LocalizationEntry);
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

        /// <summary>
        ///     Releases the preloaded Addressables handle for the given entity, freeing the cached AudioClip.
        /// </summary>
        public void ReleaseLocalizationPreload(SoundID id)
        {
            ReleaseLocalizationHandleInternal(id);
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
                PreloadLocalizationAssets(id);
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