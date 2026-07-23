using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.PlaybackGroups;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Playback Groups (max-instance cap and comb-filtering cooldown).</summary>
    public sealed class PlaybackGroupTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator PlaybackGroup_MaxInstanceCap() => new MaxInstanceCapScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator PlaybackGroup_CombFilteringCooldown() => new CombFilteringCooldownScenario().Run(new VerificationContext());
    }
}
