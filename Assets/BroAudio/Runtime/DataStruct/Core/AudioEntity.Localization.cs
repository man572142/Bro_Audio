#if PACKAGE_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Data
{
    public partial class AudioEntity
    {
        [SerializeField] private TableReference _localizationTable;
        [SerializeField] private TableEntryReference _localizationEntry;

        internal IBroAudioClip PickLocalizationClip(ClipSelectionContext context, out int index)
        {
            if (_localizationTable.ReferenceType == TableReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizationTable is not set on entity '{Name}'.");
                index = -1;
                return null;
            }

            if (_localizationEntry.ReferenceType == TableEntryReference.Type.Empty)
            {
                Debug.LogError(Utility.LogTitle + $"LocalizationEntry is not set on entity '{Name}'.");
                index = -1;
                return null;
            }

            EnsureClipSelectionStrategy<LocalizationClipStrategy>();
            var broAudioClip = _clipSelectionStrategy.SelectClip(Clips, context, out index) as BroAudioClip;
            if (broAudioClip == null)
            {
                var locale = LocalizationSettings.SelectedLocale;
                string localeName = locale != null ? locale.Identifier.ToString() : "unknown";
                Debug.LogWarning(Utility.LogTitle + $"No BroAudioClip configured for locale '{localeName}' on entity '{Name}'.");
                return null;
            }

            var handle = LocalizationSettings.AssetDatabase
                .GetLocalizedAssetAsync<AudioClip>(_localizationTable, _localizationEntry);
            var resolvedClip = handle.WaitForCompletion();

            if (resolvedClip == null)
            {
                var locale = LocalizationSettings.SelectedLocale;
                string localeName = locale != null ? locale.Identifier.ToString() : "unknown";
                Debug.LogWarning(Utility.LogTitle + $"No AudioClip set in table for locale '{localeName}' on entity '{Name}'.");
                return null;
            }

            return new LocalizedBroAudioClipWrapper(broAudioClip, resolvedClip);
        }

#if UNITY_EDITOR
        public static class LocalizationEditorPropertyName
        {
            public const string LocalizationTable = "_localizationTable";
            public const string LocalizationEntry = "_localizationEntry";
        }
#endif
    }
}
#endif
