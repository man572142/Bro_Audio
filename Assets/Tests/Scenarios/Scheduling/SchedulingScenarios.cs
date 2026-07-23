using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Scheduling
{
    /// <summary>
    /// SetScheduledStartTime(dspTime) / SetDelay(seconds) hold playback back until the target time.
    /// Per the engine facts (.claude/rules/unity-audio-engine.md): PlayScheduled reports IsPlaying == true
    /// the instant it's called, well before audible playback starts — so these scenarios assert on
    /// AudioSource.timeSamples staying at 0 during the wait, not on IsPlaying.
    /// </summary>
    internal sealed class ScheduledStartTimeScenario : IVerificationScenario
    {
        private const float DelaySeconds = 0.3f;

        public string Description => "SetScheduledStartTime holds the playhead at 0 until the scheduled dspTime arrives";
        public string ExpectedOutcome => "timeSamples stays 0 through the wait, then progresses once the scheduled time passes";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("ScheduledStart_Tone", 300f, 1f);
            var entity = TestAudioEntityFactory.Create("ScheduledStart", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            // SetScheduledStartTime calls PlayInternal() directly (AudioSource isn't playing yet), so
            // scheduling is already in effect before this frame's queued Play() even dequeues.
            player.SetScheduledStartTime(AudioSettings.dspTime + DelaySeconds);
            yield return null;

            Assert.IsTrue(player.IsPlaying, "IsPlaying reports true immediately once PlayScheduled is called, not at audible start.");
            Assert.AreEqual(0, player.AudioSource.timeSamples, "The playhead shouldn't move before the scheduled time arrives.");

            yield return new WaitForSeconds(DelaySeconds * 0.5f);
            Assert.AreEqual(0, player.AudioSource.timeSamples, "Still within the scheduled wait.");

            yield return new WaitForSeconds(DelaySeconds);
            Assert.Greater(player.AudioSource.timeSamples, 0, "Playback should be progressing once the scheduled time has passed.");

            player.Stop(0f);
        }
    }

    /// <summary>SetDelay(seconds) is a thin wrapper over SetScheduledStartTime(dspTime + seconds).</summary>
    internal sealed class SetDelayScenario : IVerificationScenario
    {
        private const float DelaySeconds = 0.3f;

        public string Description => "SetDelay(seconds) delays playback start by a relative duration";
        public string ExpectedOutcome => "timeSamples stays 0 through the delay, then progresses once it elapses";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("SetDelay_Tone", 320f, 1f);
            var entity = TestAudioEntityFactory.Create("SetDelay", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            player.SetDelay(DelaySeconds);
            yield return null;

            Assert.AreEqual(0, player.AudioSource.timeSamples);

            yield return new WaitForSeconds(DelaySeconds + 0.15f);
            Assert.Greater(player.AudioSource.timeSamples, 0, "Playback should have started once the delay elapsed.");

            player.Stop(0f);
        }
    }

    /// <summary>
    /// A clip's own Delay field delays playback the same way, without the caller calling SetDelay at all —
    /// PlayControl applies it via SetClipDelayIfNotScheduled() whenever no explicit schedule was requested.
    /// </summary>
    internal sealed class ClipDelayFieldScenario : IVerificationScenario
    {
        private const float ClipDelaySeconds = 0.25f;

        public string Description => "BroAudioClip.Delay delays playback even without an explicit SetDelay call";
        public string ExpectedOutcome => "A bare Play() on a clip with Delay > 0 still holds the playhead at 0 until the delay elapses";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("ClipDelayField_Tone", 340f, 1f, delay: ClipDelaySeconds);
            var entity = TestAudioEntityFactory.Create("ClipDelayField", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;

            Assert.AreEqual(0, player.AudioSource.timeSamples, "The clip's own Delay should hold the playhead at 0.");

            yield return new WaitForSeconds(ClipDelaySeconds + 0.15f);
            Assert.Greater(player.AudioSource.timeSamples, 0, "Playback should have started once the clip's delay elapsed.");

            player.Stop(0f);
        }
    }
}
