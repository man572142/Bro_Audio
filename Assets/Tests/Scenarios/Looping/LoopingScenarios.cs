using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Looping
{
    /// <summary>
    /// entity.WithLoop(true) produces LoopType.Loop. AudioPlayer.PlayControl schedules a handover to a fresh
    /// underlying AudioPlayer shortly before the current clip ends; when the old instance actually ends, the
    /// caller's IAudioPlayer (an AudioPlayerInstanceWrapper) transparently re-points at the new instance. So
    /// from here the *same* `player` variable should just keep reporting IsPlaying == true continuously, well
    /// past the clip's nominal duration, with no extra code needed to "follow" the handover. Gaplessness /
    /// crossfade quality is explicitly out of scope for Layer 1 (see VERIFICATION_PLAN.md, Layer 3) — this
    /// only checks the state-level claim that playback continues instead of ending.
    /// </summary>
    internal sealed class NormalLoopScenario : IVerificationScenario
    {
        private const float ClipDuration = 0.3f;

        public string Description => "A normal loop (Loop) keeps the same IAudioPlayer reporting IsPlaying past the clip's nominal duration";
        public string ExpectedOutcome => "IsPlaying stays true well beyond the clip length because playback handed over to a new instance instead of ending; Stop() then ends it";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("NormalLoop_Tone", 300f, ClipDuration);
            var entity = TestAudioEntityFactory
                .Create("NormalLoop", BroAudioType.SFX, tone)
                .WithLoop(true);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;

            yield return new WaitForSeconds(ClipDuration * 2.5f);
            Assert.IsTrue(player.IsPlaying, "A looping player should still report as playing well past the original clip's duration, since it handed over instead of ending.");

            player.Stop(0f);
            // Stop() cancels the scheduled handover coroutine for a plain (non-chained) loop, but the
            // handover chain may already be mid-flight; give it a couple of frames plus a short real-time
            // margin to fully settle before asserting the player is inactive.
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(player.IsActive, "Stop() should end the loop and recycle the player once everything settles.");
        }
    }

    /// <summary>
    /// entity.WithSeamlessLoop(transitionTime) produces LoopType.SeamlessLoop: the handover is scheduled
    /// earlier (transitionTime before the clip ends) and applies a crossfade between the outgoing and
    /// incoming instances. Neither the earlier scheduling nor the crossfade shape is observable at the
    /// state level, so — same as NormalLoopScenario — this only asserts that the same IAudioPlayer reference
    /// keeps reporting IsPlaying past the clip's nominal duration. Actual gaplessness is a Layer 3 concern.
    /// </summary>
    internal sealed class SeamlessLoopScenario : IVerificationScenario
    {
        private const float ClipDuration = 0.3f;
        private const float TransitionTime = 0.05f;

        public string Description => "A seamless loop (SeamlessLoop) keeps the same IAudioPlayer reporting IsPlaying past the clip's nominal duration";
        public string ExpectedOutcome => "IsPlaying stays true well beyond the clip length because playback handed over to a new instance instead of ending; Stop() then ends it";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("SeamlessLoop_Tone", 300f, ClipDuration);
            var entity = TestAudioEntityFactory
                .Create("SeamlessLoop", BroAudioType.SFX, tone)
                .WithSeamlessLoop(TransitionTime);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;

            yield return new WaitForSeconds(ClipDuration * 2.5f);
            Assert.IsTrue(player.IsPlaying, "A seamlessly-looping player should still report as playing well past the original clip's duration, since it handed over instead of ending.");

            player.Stop(0f);
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(player.IsActive, "Stop() should end the loop and recycle the player once everything settles.");
        }
    }
}
