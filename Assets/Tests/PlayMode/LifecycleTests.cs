using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Lifecycle;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Lifecycle / infrastructure (pooling and queries).</summary>
    public sealed class LifecycleTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Pool_ReturnsToBaselineAfterConcurrentPlays() => new PoolIntegrityScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Queries_ReflectLivePlaybackState() => new QueriesScenario().Run(new VerificationContext());
    }
}
