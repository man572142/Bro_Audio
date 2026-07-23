using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.ClipSelection;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Clip selection strategies.</summary>
    public sealed class ClipSelectionTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Single_AlwaysPicksFirstClip() => new SingleClipSelectionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Sequence_CyclesInOrderAndResets() => new SequenceClipSelectionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Random_PicksByWeight() => new RandomClipSelectionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Shuffle_CoversEveryClip() => new ShuffleClipSelectionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Velocity_PicksMatchingLayer() => new VelocityClipSelectionScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Chained_StartsOnIntroClip() => new ChainedClipSelectionScenario().Run(new VerificationContext());
    }
}
