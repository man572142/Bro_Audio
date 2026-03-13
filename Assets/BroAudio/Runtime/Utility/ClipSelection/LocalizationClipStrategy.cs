#if PACKAGE_LOCALIZATION
using Ami.BroAudio.Data;
using UnityEngine.Localization.Settings;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Selects the <see cref="BroAudioClip"/> whose <c>Locale</c> matches
    /// <see cref="LocalizationSettings.SelectedLocale"/>. Returns <c>null</c> when no
    /// matching clip is found (the caller is responsible for logging the warning).
    /// </summary>
    public class LocalizationClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            var selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].Locale == selectedLocale.Identifier)
                    {
                        index = i;
                        return clips[i];
                    }
                }
            }

            index = -1;
            return null;
        }

        public void Reset() { }
    }
}
#endif
