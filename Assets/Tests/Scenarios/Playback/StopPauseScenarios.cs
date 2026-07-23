using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Playback
{
    /// <summary>Stop(fadeOut: 0) ends playback and recycles the player in the same frame it's called.</summary>
    internal sealed class StopImmediateScenario : IVerificationScenario
    {
        public string Description => "Stop with no fade-out ends playback immediately";
        public string ExpectedOutcome => "The player is no longer active right after Stop() returns, with no extra frame needed";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("StopImmediate_Tone", 400f, 1f);
            var entity = TestAudioEntityFactory.Create("StopImmediate", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;
            Assert.IsTrue(player.IsPlaying);

            player.Stop(0f);
            // An explicit 0-second fade-out resolves as an immediate stop; StopControl's coroutine runs
            // synchronously to EndPlaying() with no yield in between, so recycling is already done here.
            Assert.IsFalse(player.IsActive, "A zero fade-out should end and recycle the player without waiting a frame.");

            yield return null;
        }
    }

    /// <summary>Stop(fadeOut > 0): GetVolume() ramps down to 0 before the player is finally recycled.</summary>
    internal sealed class StopFadeOutScenario : IVerificationScenario
    {
        private const float FadeOutSeconds = 0.3f;

        public string Description => "Stop(fadeOut) ramps the composite volume down before ending playback";
        public string ExpectedOutcome => "GetVolume() decreases during the fade, and the player is recycled once it completes";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("StopFadeOut_Tone", 420f, 2f);
            var entity = TestAudioEntityFactory.Create("StopFadeOut", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;
            Assert.AreEqual(1f, player.GetVolume(), 0.01f);

            player.Stop(FadeOutSeconds);
            yield return new WaitForSeconds(FadeOutSeconds * 0.5f);

            Assert.IsTrue(player.IsActive, "The player should still be active mid fade-out.");
            Assert.Less(player.GetVolume(), 0.95f, "Volume should have started dropping mid fade-out.");

            yield return new WaitForSeconds(FadeOutSeconds + 0.1f);
            Assert.IsFalse(player.IsActive, "The player should be recycled once the fade-out completes.");
        }
    }

    /// <summary>Pause/UnPause by id preserve the exact playhead across the pause.</summary>
    internal sealed class PauseUnPauseScenario : IVerificationScenario
    {
        public string Description => "Pause freezes playback and UnPause resumes from the same sample";
        public string ExpectedOutcome => "IsPlaying flips false/true across pause/unpause, and timeSamples doesn't rewind";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PauseUnPause_Tone", 380f, 2f);
            var entity = TestAudioEntityFactory.Create("PauseUnPause", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;
            yield return new WaitForSeconds(0.1f);

            int samplesBeforePause = player.AudioSource.timeSamples;
            player.Pause();

            // The clip's own FadeOut is 0 and Pause() applies no override, so TryGetFadeOut resolves to
            // "no fade" and StopControl's coroutine runs synchronously through to AudioSource.Pause().
            Assert.IsFalse(player.IsPlaying, "Pause should stop the source immediately when the clip has no fade-out.");
            Assert.AreEqual(samplesBeforePause, player.AudioSource.timeSamples, "Pausing shouldn't move the playhead.");

            yield return new WaitForSeconds(0.2f); // Time passing while paused must not advance the playhead.
            Assert.AreEqual(samplesBeforePause, player.AudioSource.timeSamples, "The playhead must stay frozen while paused.");

            player.UnPause();
            yield return null;

            Assert.IsTrue(player.IsPlaying, "UnPause should resume playback.");
            Assert.GreaterOrEqual(player.AudioSource.timeSamples, samplesBeforePause, "Resuming should continue from the paused sample, not rewind to 0.");

            player.Stop(0f);
        }
    }

    /// <summary>Stopping while a fade-in is still in progress must end playback cleanly, with no leaked player.</summary>
    internal sealed class StopDuringFadeInScenario : IVerificationScenario
    {
        public string Description => "Stop() cancels an in-progress fade-in without leaking the player";
        public string ExpectedOutcome => "The player becomes inactive right after Stop(), even mid fade-in";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("StopDuringFadeIn_Tone", 340f, 2f);
            var entity = TestAudioEntityFactory.Create("StopDuringFadeIn", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id, 1f);
            yield return null;
            yield return new WaitForSeconds(0.2f);

            Assert.IsTrue(player.IsActive, "Should still be mid fade-in at this point.");
            Assert.Less(player.GetVolume(), 0.95f);

            player.Stop(0f);
            Assert.IsFalse(player.IsActive, "Stopping mid fade-in should still end and recycle the player cleanly.");

            yield return null;
        }
    }
}
