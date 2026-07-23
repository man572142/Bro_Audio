using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Volume;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Volume / Pitch.</summary>
    public sealed class VolumePitchTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator SetVolume_ImmediateAndFaded() => new SetVolumeScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator SetPitch_ImmediateAndFaded() => new SetPitchScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator SetPitch_ExplicitOverridesRandomization() => new ExplicitPitchOverridesRandomizationScenario().Run(new VerificationContext());
    }
}
