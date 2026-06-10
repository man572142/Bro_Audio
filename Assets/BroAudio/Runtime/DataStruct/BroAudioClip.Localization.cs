#if PACKAGE_LOCALIZATION
using UnityEngine;
using UnityEngine.Localization;

namespace Ami.BroAudio.Data
{
    public partial class BroAudioClip
    {
        [SerializeField] public LocaleIdentifier Locale;
    }
}
#endif
