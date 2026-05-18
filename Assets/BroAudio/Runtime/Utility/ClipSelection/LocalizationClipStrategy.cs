#if PACKAGE_LOCALIZATION
using Ami.BroAudio.Data;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Selects the <see cref="BroAudioClip"/> whose <c>Locale</c> matches
    /// <see cref="LocalizationSettings.SelectedLocale"/> and resolves the audio clip
    /// from the Unity Asset Table. Call <see cref="Inject"/> before each use.
    /// </summary>
    public class LocalizationClipStrategy : IClipSelectionStrategy
    {
        private TableReference _table;
        private TableEntryReference _entry;
        private string _entityName;

        public void Inject(TableReference table, TableEntryReference entry, string entityName)
        {
            _table = table;
            _entry = entry;
            _entityName = entityName;
        }

        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            if (_table.ReferenceType == TableReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizationTable is not set on entity '{_entityName}'.");
                index = -1;
                return null;
            }

            if (_entry.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizationEntry is not set on entity '{_entityName}'.");
                index = -1;
                return null;
            }

            var handle = LocalizationSettings.AssetDatabase
                .GetLocalizedAssetAsync<AudioClip>(_table, _entry);
            if (!handle.IsDone)
            {
                Debug.LogWarning(Utility.LogTitle +
                    $"Localized AudioClip for entity '{_entityName}' was not preloaded; " +
                    $"resolving synchronously will block the main thread. " +
                    $"Call {nameof(SoundManager)}.{nameof(SoundManager.PreloadLocalizationAssets)}(SoundID) before playback to avoid hitches.");
            }
            var resolvedClip = handle.WaitForCompletion();
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
