#if PACKAGE_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization;

namespace Ami.BroAudio.Data
{
    public partial class AudioEntity
    {
        [SerializeField] private LocalizedAudioClip _localizedAudio;

        public LocalizedAudioClip LocalizedAudio => _localizedAudio;

#if UNITY_EDITOR
        public static class LocalizationEditorPropertyName
        {
            public const string LocalizedAudio = "_localizedAudio";
            public const string Table          = "_localizedAudio.m_TableReference";
            public const string Entry          = "_localizedAudio.m_TableEntryReference";
        }
#endif
    }
}
#endif
