using System.Collections;
using Ami.BroAudio.Tests.Scenarios;
using Ami.BroAudio.Tests.Scenarios.Playback;
using UnityEngine.TestTools;

namespace Ami.BroAudio.Tests.PlayMode
{
    /// <summary>Layer 1 coverage matrix: Play verbs, and Stop / Pause / UnPause.</summary>
    public sealed class PlaybackTests : BroAudioPlayModeTestBase
    {
        [UnityTest]
        public IEnumerator Play_Basic() => new PlayBasicScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Play_FadeIn() => new PlayFadeInScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Play_Spatial() => new PlaySpatialScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Play_FollowTarget() => new PlayFollowTargetScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Stop_Immediate() => new StopImmediateScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Stop_FadeOut() => new StopFadeOutScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Pause_UnPause() => new PauseUnPauseScenario().Run(new VerificationContext());

        [UnityTest]
        public IEnumerator Stop_DuringFadeIn() => new StopDuringFadeInScenario().Run(new VerificationContext());
    }
}
