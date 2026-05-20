#if PACKAGE_LOCALIZATION
using System;
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
        private Dictionary<SoundID, LocalizedAsset<AudioClip>> _localizedAssets;
        private Dictionary<(SoundID id, Action<SoundID> handler), LocalizedAsset<AudioClip>.ChangeHandler> _localizedClipHandlers;

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
            if (!HasValidLocalizationReferences(audioEntity))
            {
                return default;
            }

            _preloadedLocalizationHandles ??= new Dictionary<SoundID, AsyncOperationHandle<AudioClip>>();

            if (_preloadedLocalizationHandles.TryGetValue(id, out var cached) && cached.IsValid())
            {
                return cached;
            }

            if (!_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = true;
            }

            var handle = LocalizationSettings.AssetDatabase
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
            var clipHandle = LoadLocalizedAssetAsync(id, audioEntity);
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

        private bool IsLocalizationHandleLoaded(SoundID id)
        {
            return _preloadedLocalizationHandles != null
                && _preloadedLocalizationHandles.TryGetValue(id, out var handle)
                && handle.IsValid()
                && handle.IsDone;
        }

        private void ReleaseLocalizationHandleInternal(SoundID id)
        {
            if (_preloadedLocalizationHandles == null)
            {
                return;
            }

            if (_preloadedLocalizationHandles.TryGetValue(id, out var handle))
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

            foreach (var handle in _preloadedLocalizationHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _preloadedLocalizationHandles.Clear();
        }

        private void ReleaseAllLocalizationPreloads()
        {
            if (_preloadedLocalizationHandles != null)
            {
                foreach (var handle in _preloadedLocalizationHandles.Values)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }

                _preloadedLocalizationHandles.Clear();
            }

            if (_localizedClipHandlers != null && _localizedAssets != null)
            {
                foreach (var kv in _localizedClipHandlers)
                {
                    if (_localizedAssets.TryGetValue(kv.Key.id, out var asset))
                    {
                        asset.AssetChanged -= kv.Value;
                    }
                }
            }

            _localizedClipHandlers?.Clear();
            _localizedAssets?.Clear();

            if (_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = false;
            }
        }

        public void SubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (!TryGetLocalizationEntity(id, out var entity))
            {
                Debug.LogWarning(Utility.LogTitle +
                                 $"SubscribeLocalizedClipChanged: entity for SoundID '{id}' is not in Localization mode.");
                return;
            }

            if (!HasValidLocalizationReferences(entity))
            {
                return;
            }

            _localizedAssets ??= new Dictionary<SoundID, LocalizedAsset<AudioClip>>();
            _localizedClipHandlers ??= new Dictionary<(SoundID, Action<SoundID>), LocalizedAsset<AudioClip>.ChangeHandler>();

            var key = (id, handler);
            if (_localizedClipHandlers.ContainsKey(key))
            {
                return;
            }

            if (!_localizedAssets.TryGetValue(id, out var localizedAsset))
            {
                localizedAsset = new LocalizedAsset<AudioClip>
                {
                    TableReference = entity.LocalizationTable,
                    TableEntryReference = entity.LocalizationEntry,
                };
                _localizedAssets[id] = localizedAsset;
            }

            LocalizedAsset<AudioClip>.ChangeHandler wrapper = _ => handler(id);
            _localizedClipHandlers[key] = wrapper;
            localizedAsset.AssetChanged += wrapper;
        }

        public void UnsubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)
        {
            if (handler == null || _localizedClipHandlers == null)
            {
                return;
            }

            var key = (id, handler);
            if (!_localizedClipHandlers.TryGetValue(key, out var wrapper))
            {
                return;
            }

            if (_localizedAssets != null && _localizedAssets.TryGetValue(id, out var localizedAsset))
            {
                localizedAsset.AssetChanged -= wrapper;
            }

            _localizedClipHandlers.Remove(key);

            bool anyRemainingForId = false;
            foreach (var k in _localizedClipHandlers.Keys)
            {
                if (k.id.Equals(id))
                {
                    anyRemainingForId = true;
                    break;
                }
            }

            if (!anyRemainingForId)
            {
                _localizedAssets?.Remove(id);
            }
        }

        private bool TryGetLocalizationEntity(SoundID id, out AudioEntity entity)
        {
            entity = null;
            if (TryGetEntity(id, out var e) && e is AudioEntity ae
                && ae.PlayMode == MulticlipsPlayMode.Localization)
            {
                entity = ae;
                return true;
            }
            return false;
        }

        private static bool HasValidLocalizationReferences(AudioEntity audioEntity)
        {
            if (audioEntity.LocalizationTable.ReferenceType == TableReference.Type.Empty ||
                audioEntity.LocalizationEntry.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogWarning(Utility.LogTitle +
                                 $"LocalizationTable or LocalizationEntry is not set on entity '{audioEntity.Name}'.");
                return false;
            }
            return true;
        }
    }
}
#endif