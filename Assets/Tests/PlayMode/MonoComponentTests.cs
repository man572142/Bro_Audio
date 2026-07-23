using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.MonoComponents;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: MonoComponents (SoundSource).</summary>
    public sealed class MonoComponentTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SoundSource_PlaysAndStops() => new SoundSourcePlaysAndStopsScenario().Run(new VerificationContext());
    }
}
