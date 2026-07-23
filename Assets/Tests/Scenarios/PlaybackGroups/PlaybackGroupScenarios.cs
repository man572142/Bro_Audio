using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.PlaybackGroups
{
    /// <summary>
    /// SoundManager.Play only creates a real, active player if entity.PlaybackGroup.IsPlayable(id, position)
    /// returns true; a rejected play returns the stub Empty.AudioPlayer instead (IsPlaying/IsActive always
    /// false, every other member a safe no-op — so Stop(0f) is always safe to call on it too). This scenario
    /// drives DefaultPlaybackGroup's Max Playable Count rule: once _currentPlayingCount reaches the cap,
    /// further plays are rejected until DefaultPlaybackGroup.OnGetPlayer's OnEnd subscription decrements the
    /// count again when one of the active players stops.
    /// </summary>
    internal sealed class MaxInstanceCapScenario : IVerificationScenario
    {
        public string Description => "A PlaybackGroup's Max Playable Count rule rejects plays once the cap is reached, then allows them again once a slot frees up";
        public string ExpectedOutcome => "The 3rd concurrent play is rejected (Empty.AudioPlayer, IsPlaying false) while 2 are active; stopping one frees a slot for the next play to succeed";

        public IEnumerator Run(VerificationContext context)
        {
            var group = TestPlaybackGroupFactory.Create(maxPlayableCount: 2, combFilteringTime: 0f);
            var tone = TestClipFactory.CreateTone("MaxInstanceCap_Tone", 300f, 2f);
            var entity = TestAudioEntityFactory
                .Create("MaxInstanceCap", BroAudioType.SFX, tone)
                .WithPlaybackGroup(group);
            var id = new SoundID(entity);

            IAudioPlayer first = BroAudio.Play(id);
            yield return null;
            Assert.IsTrue(first.IsPlaying, "The 1st concurrent play should be within the cap of 2.");

            IAudioPlayer second = BroAudio.Play(id);
            yield return null;
            Assert.IsTrue(second.IsPlaying, "The 2nd concurrent play should be within the cap of 2.");

            IAudioPlayer third = BroAudio.Play(id);
            yield return null;
            Assert.IsFalse(third.IsPlaying, "The 3rd concurrent play should be rejected by the Max Playable Count rule.");

            first.Stop(0f);
            // The group's rule decrements its internal playing-count via an automatic OnEnd callback
            // (DefaultPlaybackGroup.OnGetPlayer subscribed it when `first` was granted) — nothing extra needed.
            yield return null;

            IAudioPlayer fourth = BroAudio.Play(id);
            yield return null;
            Assert.IsTrue(fourth.IsPlaying, "Freeing a slot by stopping one active player should let the next play succeed again.");

            second.Stop(0f);
            fourth.Stop(0f);
            third.Stop(0f); // Safe no-op: a rejected play is the Empty.AudioPlayer stub.
            yield return null;
        }
    }

    /// <summary>
    /// Drives DefaultPlaybackGroup's Comb-Filtering rule: a repeat play on the same id is rejected while
    /// within the cooldown window, and allowed again once it elapses. The rule's timing reference
    /// (AudioPlayer.PlaybackStartingTime) is only set once the queued player actually starts playing in
    /// SoundManager's LateUpdate, so the first play needs a `yield return null` before the timing-sensitive
    /// second play attempt — otherwise the rule would read the reference as "still queued" instead of
    /// "started this frame", which resolves through a different branch of HasPassedCombFilteringRule.
    /// </summary>
    internal sealed class CombFilteringCooldownScenario : IVerificationScenario
    {
        private const float CombFilteringTime = 0.5f;

        public string Description => "A PlaybackGroup's Comb-Filtering rule rejects an immediate repeat play on the same id, then allows it again once the cooldown elapses";
        public string ExpectedOutcome => "A play attempted the same frame the previous one started is rejected; the same play succeeds again once past the cooldown";

        public IEnumerator Run(VerificationContext context)
        {
            var group = TestPlaybackGroupFactory.Create(maxPlayableCount: -1, combFilteringTime: CombFilteringTime);
            var tone = TestClipFactory.CreateTone("CombFiltering_Tone", 300f, 2f);
            var entity = TestAudioEntityFactory
                .Create("CombFiltering", BroAudioType.SFX, tone)
                .WithPlaybackGroup(group);
            var id = new SoundID(entity);

            IAudioPlayer first = BroAudio.Play(id);
            yield return null; // Let it actually start so PlaybackStartingTime is set before the next play attempt.
            Assert.IsTrue(first.IsPlaying, "The first play should succeed (nothing to collide with yet).");

            IAudioPlayer second = BroAudio.Play(id); // Same frame as first's start, no extra yield: well within the cooldown.
            Assert.IsFalse(second.IsPlaying, "A play attempted within the comb-filtering cooldown should be rejected.");

            yield return new WaitForSeconds(CombFilteringTime + 0.1f); // Comfortably past the cooldown.

            IAudioPlayer third = BroAudio.Play(id);
            yield return null;
            Assert.IsTrue(third.IsPlaying, "A play attempted after the cooldown has elapsed should succeed.");

            first.Stop(0f);
            third.Stop(0f);
            second.Stop(0f); // Safe no-op: a rejected play is the Empty.AudioPlayer stub.
            yield return null;
        }
    }
}
