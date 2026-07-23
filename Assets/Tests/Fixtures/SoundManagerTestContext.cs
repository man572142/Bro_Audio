using System.Collections;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Tests.Fixtures
{
    /// <summary>
    /// Boots and resets the real, singleton <see cref="SoundManager"/> for PlayMode tests, per
    /// VERIFICATION_PLAN.md guiding principle #1 (drive the public contract through a real SoundManager,
    /// not test doubles). Unless <c>BroAudio_InitManually</c> is defined, SoundManager already
    /// auto-bootstraps via [RuntimeInitializeOnLoadMethod] before this ever runs; <see cref="EnsureReady"/>
    /// only needs to cover the manual-init path and the first-frame AudioMixer.SetFloat warm-up.
    /// SoundManager is DontDestroyOnLoad and persists for the whole Play Mode session, so tests share one
    /// instance and must reset playback state between runs via <see cref="StopAll"/> instead of re-booting.
    /// </summary>
    internal static class SoundManagerTestContext
    {
        internal static IEnumerator EnsureReady()
        {
            if (!SoundManager.HasInstance)
            {
                SoundManager.Init();
            }

            // AudioMixer.SetFloat silently fails in Awake/OnEnable on the first Play Mode frame.
            yield return null;
        }

        internal static void StopAll()
        {
            BroAudio.Stop(BroAudioType.All, 0f);
        }
    }
}
