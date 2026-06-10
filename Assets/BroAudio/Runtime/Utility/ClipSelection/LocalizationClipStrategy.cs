#if PACKAGE_LOCALIZATION
using System;
using Ami.BroAudio.Data;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Selects the <see cref="BroAudioClip"/> whose <c>Locale</c> matches
    /// <see cref="LocalizationSettings.SelectedLocale"/> and resolves the audio clip
    /// from the entity's <see cref="LocalizedAudioClip"/>. Call <see cref="Inject"/> before each use.
    /// </summary>
    public class LocalizationClipStrategy : IClipSelectionStrategy
    {
        private LocalizedAudioClip _localizedAudio;
        private string _entityName;
        private Func<AudioClip> _tryGetCachedClip;

        public void Inject(LocalizedAudioClip localizedAudio, string entityName, Func<AudioClip> tryGetCachedClip)
        {
            _localizedAudio = localizedAudio;
            _entityName = entityName;
            _tryGetCachedClip = tryGetCachedClip;
        }

        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            if (_localizedAudio == null
                || _localizedAudio.TableReference.ReferenceType == TableReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizedAudio table is not set on entity '{_entityName}'.");
                index = -1;
                return null;
            }

            if (_localizedAudio.TableEntryReference.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizedAudio entry is not set on entity '{_entityName}'.");
                index = -1;
                return null;
            }

            AudioClip resolvedClip = _tryGetCachedClip?.Invoke();
            if (resolvedClip == null)
            {
                var handle = _localizedAudio.LoadAssetAsync();
                if (!handle.IsDone)
                {
                    Debug.LogWarning(Utility.LogTitle +
                        $"Localized AudioClip for entity '{_entityName}' was not preloaded; " +
                        $"resolving synchronously will block the main thread until the clip is loaded. " +
                        $"Call {nameof(BroAudio)}.{nameof(BroAudio.LoadAssetAsync)}(SoundID) before playback to avoid hitches.");
                }
                resolvedClip = handle.WaitForCompletion();
            }

            var selectedLocale = LocalizationSettings.SelectedLocale;

            if (resolvedClip == null)
            {
                string localeName = selectedLocale != null ? selectedLocale.Identifier.ToString() : "unknown";
                Debug.LogWarning(Utility.LogTitle + $"No AudioClip set in table for locale '{localeName}' on entity '{_entityName}'.");
                index = -1;
                return null;
            }

            if (selectedLocale != null && clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].Locale == selectedLocale.Identifier)
                    {
                        index = i;
                        return new LocalizedBroAudioClipWrapper(clips[i], resolvedClip);
                    }
                }
            }

            string activeCode = selectedLocale != null ? selectedLocale.Identifier.ToString() : "unknown";
            Debug.LogWarning(Utility.LogTitle + $"No BroAudioClip row found for locale '{activeCode}' on entity '{_entityName}'. Playback properties will use defaults.");
            index = 0;
            return new LocalizedBroAudioClipWrapper(resolvedClip);
        }

        public void Reset() { }
    }
}
#endif
