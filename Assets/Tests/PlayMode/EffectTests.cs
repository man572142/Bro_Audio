using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Effects;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Effects (BroAudio.SetEffect, global and by-type).</summary>
    public sealed class EffectTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SetEffect_LowPassMovesAndRestores() => new SetEffectLowPassScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator SetEffect_ByTypeHighPassMovesAndRestores() => new SetEffectByTypeScenario().Run(new VerificationContext());
    }
}
