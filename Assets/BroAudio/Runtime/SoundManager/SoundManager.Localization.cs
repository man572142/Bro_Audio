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
        private Dictionary<SoundID, LocalizedRuntimeEntry> _localizedRuntime;

        private sealed class LocalizedRuntimeEntry
        {
            public AudioEntity Entity;
            public AudioClip CurrentClip;
            public AsyncOperationHandle<AudioClip> PreloadHandle;
            public LocalizedAsset<AudioClip>.ChangeHandler Tracker;
            public Dictionary<Action<SoundID>, LocalizedAsset<AudioClip>.ChangeHandler> UserHandlers;
        }

        /// <summary>
        /// Resolves the locale-correct AudioClip for the given Localization-mode entity. Returns a handle
        /// whose <c>Completed</c> fires once the asset is loaded. The cache holds the clip via either the
        /// preload handle or an AssetChanged subscription; <see cref="ReleaseLocalizationClipInternal"/>
        /// releases the preload contribution.
        /// </summary>
        private AsyncOperationHandle<AudioClip> LoadLocalizedAssetAsync(SoundID id, AudioEntity audioEntity)
        {
            if (!HasValidLocalizationReferences(audioEntity.LocalizedAudio, audioEntity.Name))
            {
                return default;
            }

            var entry = GetOrCreateEntry(id, audioEntity);

            if (entry.PreloadHandle.IsValid())
            {
                return entry.CurrentClip != null
                    ? Addressables.ResourceManager.CreateCompletedOperation(entry.CurrentClip, string.Empty)
                    : entry.PreloadHandle;
            }

            entry.PreloadHandle = audioEntity.LocalizedAudio.LoadAssetAsync();
            return entry.PreloadHandle;
        }

        /// <summary>
        /// Wraps the single resolved localized clip into an <see cref="IList{AudioClip}"/> handle so the
        /// return type matches the Addressables path of <see cref="LoadAllAssetsAsync"/>.
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

        private bool IsLocalizationClipLoaded(SoundID id)
        {
            return _localizedRuntime != null
                && _localizedRuntime.TryGetValue(id, out var entry)
                && entry.PreloadHandle.IsValid()
                && entry.CurrentClip != null;
        }

        private void ReleaseLocalizationClipInternal(SoundID id)
        {
            if (_localizedRuntime == null
                || !_localizedRuntime.TryGetValue(id, out var entry)
                || !entry.PreloadHandle.IsValid())
            {
                return;
            }

            ReleaseUnderlyingAsset(entry.Entity.LocalizedAudio);

            entry.PreloadHandle = default;
            MaybeTearDownEntry(id, entry);
        }

        private void OnSelectedLocaleChanged(Locale newLocale)
        {
            if (_localizedRuntime == null) return;

            List<SoundID> ghosts = null;
            foreach (var kv in _localizedRuntime)
            {
                var entry = kv.Value;
                entry.CurrentClip = null;
                if (entry.PreloadHandle.IsValid())
                {
                    // Defensive: covers the rare in-flight load at the moment of locale switch.
                    // Once Unity completes the switch the handle is invalidated naturally.
                    Addressables.Release(entry.PreloadHandle);
                    entry.PreloadHandle = default;
                }
                if (entry.UserHandlers.Count == 0)
                {
                    (ghosts ??= new List<SoundID>()).Add(kv.Key);
                }
            }

            if (ghosts != null)
            {
                foreach (var id in ghosts)
                {
                    MaybeTearDownEntry(id, _localizedRuntime[id]);
                }
            }
            // For surviving entries, Unity re-fires AssetChanged for the new locale and the
            // Tracker repopulates CurrentClip.
        }

        private void ReleaseAllLocalizationPreloads()
        {
            if (_localizedRuntime != null)
            {
                foreach (var entry in _localizedRuntime.Values)
                {
                    if (entry.PreloadHandle.IsValid())
                    {
                        ReleaseUnderlyingAsset(entry.Entity.LocalizedAudio);
                    }

                    foreach (var kv in entry.UserHandlers)
                    {
                        entry.Entity.LocalizedAudio.AssetChanged -= kv.Value;
                    }

                    entry.Entity.LocalizedAudio.AssetChanged -= entry.Tracker;
                }
                _localizedRuntime.Clear();
            }

            if (_isSubscribedToLocaleChanged)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                _isSubscribedToLocaleChanged = false;
            }
        }

        public void SubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)
        {
            if (handler == null) return;

            if (!TryGetLocalizationEntity(id, out var entity))
            {
                Debug.LogWarning(Utility.LogTitle +
                    $"SubscribeLocalizedAudioChanged: entity for SoundID '{id}' is not in Localization mode.");
                return;
            }

            if (!HasValidLocalizationReferences(entity.LocalizedAudio, entity.Name))
            {
                return;
            }

            var entry = GetOrCreateEntry(id, entity);
            if (entry.UserHandlers.ContainsKey(handler))
            {
                return;
            }

            LocalizedAsset<AudioClip>.ChangeHandler wrapper = _ => handler(id);
            entity.LocalizedAudio.AssetChanged += wrapper;
            entry.UserHandlers[handler] = wrapper;
        }

        public void UnsubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)
        {
            if (handler == null || _localizedRuntime == null) return;

            if (!_localizedRuntime.TryGetValue(id, out var entry)) return;
            if (!entry.UserHandlers.TryGetValue(handler, out var wrapper)) return;

            entry.Entity.LocalizedAudio.AssetChanged -= wrapper;
            entry.UserHandlers.Remove(handler);
            MaybeTearDownEntry(id, entry);
        }

        internal bool TryGetCachedLocalizedClip(SoundID id, out AudioClip clip)
        {
            clip = null;
            if (_localizedRuntime != null
                && _localizedRuntime.TryGetValue(id, out var entry)
                && entry.CurrentClip != null)
            {
                clip = entry.CurrentClip;
                return true;
            }
            return false;
        }

        private LocalizedRuntimeEntry GetOrCreateEntry(SoundID id, AudioEntity entity)
        {
            _localizedRuntime ??= new Dictionary<SoundID, LocalizedRuntimeEntry>();
            if (_localizedRuntime.TryGetValue(id, out var entry))
            {
                return entry;
            }

            EnsureLocaleChangedSubscribed();

            entry = new LocalizedRuntimeEntry
            {
                Entity = entity,
                UserHandlers = new Dictionary<Action<SoundID>, LocalizedAsset<AudioClip>.ChangeHandler>(),
            };
            entry.Tracker = clip => entry.CurrentClip = clip;
            entity.LocalizedAudio.AssetChanged += entry.Tracker;
            _localizedRuntime[id] = entry;
            return entry;
        }

        private void MaybeTearDownEntry(SoundID id, LocalizedRuntimeEntry entry)
        {
            if (entry.PreloadHandle.IsValid() || entry.UserHandlers.Count > 0)
            {
                return;
            }

            entry.Entity.LocalizedAudio.AssetChanged -= entry.Tracker;
            _localizedRuntime.Remove(id);
        }

        private void EnsureLocaleChangedSubscribed()
        {
            if (_isSubscribedToLocaleChanged) return;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            _isSubscribedToLocaleChanged = true;
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

        private static bool HasValidLocalizationReferences(LocalizedAudioClip localizedAudio, string entityName)
        {
            if (localizedAudio == null
                || localizedAudio.TableReference.ReferenceType == TableReference.Type.Empty
                || localizedAudio.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogWarning(Utility.LogTitle +
                    $"LocalizedAudio table or entry is not set on entity '{entityName}'.");
                return false;
            }
            return true;
        }

        private static void ReleaseUnderlyingAsset(LocalizedAudioClip localizedAudio)
        {
            var table = LocalizationSettings.AssetDatabase.GetTable(localizedAudio.TableReference);
            if (table != null)
            {
                table.ReleaseAsset(localizedAudio.TableEntryReference);
            }
        }
    }
}
#endif
