using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Scheduling;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Scheduling (SetScheduledStartTime, SetDelay, clip.Delay).</summary>
    public sealed class SchedulingTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SetScheduledStartTime_HoldsPlayheadUntilDspTime() => new ScheduledStartTimeScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator SetDelay_HoldsPlayheadForRelativeDuration() => new SetDelayScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator ClipDelayField_HoldsPlayheadWithoutExplicitCall() => new ClipDelayFieldScenario().Run(new VerificationContext());
    }
}
