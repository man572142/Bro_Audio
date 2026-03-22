#if PACKAGE_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Ami.BroAudio.Data
{
    public partial class AudioEntity
    {
        [SerializeField] private TableReference _localizationTable;
        [SerializeField] private TableEntryReference _localizationEntry;

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
