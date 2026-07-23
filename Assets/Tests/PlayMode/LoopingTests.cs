using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Looping;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Looping (normal loop and seamless loop).</summary>
    public sealed class LoopingTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Loop_Normal() => new NormalLoopScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Loop_Seamless() => new SeamlessLoopScenario().Run(new VerificationContext());
    }
}
