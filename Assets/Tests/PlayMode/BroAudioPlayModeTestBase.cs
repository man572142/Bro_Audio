using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>
    /// Shared PlayMode setup: boot the real <see cref="Ami.BroAudio.Runtime.SoundManager"/> and stop all
    /// playback between tests. SoundManager is a DontDestroyOnLoad singleton that persists for the whole
    /// Play Mode session, so tests share one instance instead of each getting a fresh boot.
    /// </summary>
    public abstract class BroAudioPlayModeTestBase
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SoundManagerTestContext.EnsureReady();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SoundManagerTestContext.StopAll();
            yield return null;
        }
    }
}
