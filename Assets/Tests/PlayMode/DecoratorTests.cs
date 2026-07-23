using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Decorators;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Chaining / decorators (AsBGM, AsDominator).</summary>
    public sealed class DecoratorTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Bgm_ImmediateTransitionSwapsCurrentBgm() => new BgmTransitionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Dominator_QuietOthersDucksAndRestores() => new DominatorQuietOthersScenario().Run(new VerificationContext());
    }
}
