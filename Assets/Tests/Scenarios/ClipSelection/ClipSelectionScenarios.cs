using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;

namespace Ami.BroAudio.Tests.Scenarios.ClipSelection
{
    /// <summary>MulticlipsPlayMode.Single always plays the first clip.</summary>
    internal sealed class SingleClipSelectionScenario : IVerificationScenario
    {
        public string Description => "MulticlipsPlayMode.Single always picks the first clip";
        public string ExpectedOutcome => "Every play selects clips[0], regardless of repetition";

        public IEnumerator Run(VerificationContext context)
        {
            var first = TestClipFactory.CreateTone("Single_First", 300f, 0.3f);
            var second = TestClipFactory.CreateTone("Single_Second", 500f, 0.3f);
            var entity = TestAudioEntityFactory
                .Create("Single", BroAudioType.SFX, first, second)
                .WithPlayMode(MulticlipsPlayMode.Single);
            var id = new SoundID(entity);

            for (int i = 0; i < 3; i++)
            {
                IAudioPlayer player = BroAudio.Play(id);
                yield return null;
                Assert.AreSame(first, player.CurrentPlayingClip, $"Play #{i} should still pick the first clip.");
                player.Stop(0f);
                yield return null;
            }
        }
    }

    /// <summary>
    /// MulticlipsPlayMode.Sequence cycles clips in order and wraps around; ResetMultiClipStrategy(id) restarts
    /// the default sequence; a named sequence (via SetSequenceId) tracks independently of the default one.
    /// </summary>
    internal sealed class SequenceClipSelectionScenario : IVerificationScenario
    {
        public string Description => "MulticlipsPlayMode.Sequence cycles clips in order, and named sequences track independently";
        public string ExpectedOutcome => "Default plays advance 0,1,2,0,...; ResetMultiClipStrategy(id) restarts it; a named sequence starts its own count from 0";

        public IEnumerator Run(VerificationContext context)
        {
            var clips = new[]
            {
                TestClipFactory.CreateTone("Sequence_0", 300f, 0.3f),
                TestClipFactory.CreateTone("Sequence_1", 400f, 0.3f),
                TestClipFactory.CreateTone("Sequence_2", 500f, 0.3f),
            };
            var entity = TestAudioEntityFactory
                .Create("Sequence", BroAudioType.SFX, clips)
                .WithPlayMode(MulticlipsPlayMode.Sequence);
            var id = new SoundID(entity);

            // Default sequence: 0, 1, 2, then wraps back to 0.
            int[] expectedOrder = { 0, 1, 2, 0 };
            foreach (int expectedIndex in expectedOrder)
            {
                IAudioPlayer player = BroAudio.Play(id);
                yield return null;
                Assert.AreSame(clips[expectedIndex], player.CurrentPlayingClip);
                player.Stop(0f);
                yield return null;
            }

            // The default sequence is now positioned at index 0 (just played). Advance it once more to index 1...
            IAudioPlayer advanced = BroAudio.Play(id);
            yield return null;
            Assert.AreSame(clips[1], advanced.CurrentPlayingClip);
            advanced.Stop(0f);
            yield return null;

            // ...then reset it and confirm the next play restarts from clips[0].
            BroAudio.ResetMultiClipStrategy(id);
            IAudioPlayer afterReset = BroAudio.Play(id);
            yield return null;
            Assert.AreSame(clips[0], afterReset.CurrentPlayingClip, "ResetMultiClipStrategy(id) should restart the default sequence.");
            afterReset.Stop(0f);
            yield return null;

            // A named sequence tracks its own position, independent of the default sequence's current index.
            IAudioPlayer named = BroAudio.Play(id);
            named.SetSequenceId("alt-sequence"); // Must be set before the queued Play() runs so PickNewClip sees it.
            yield return null;
            Assert.AreSame(clips[0], named.CurrentPlayingClip, "A fresh named sequence should start at clips[0].");
            named.Stop(0f);
            yield return null;
        }
    }

    /// <summary>
    /// MulticlipsPlayMode.Random picks by weight. Giving one clip an overwhelming weight and the rest 0
    /// makes the pick deterministic (RandomClipStrategy's weighted branch always lands in that clip's bucket),
    /// avoiding a flaky, statistics-based assertion.
    /// </summary>
    internal sealed class RandomClipSelectionScenario : IVerificationScenario
    {
        public string Description => "MulticlipsPlayMode.Random picks by weight";
        public string ExpectedOutcome => "A clip with overwhelmingly larger weight than its siblings is always picked";

        public IEnumerator Run(VerificationContext context)
        {
            var dominant = TestClipFactory.CreateTone("Random_Dominant", 300f, 0.3f, weight: 1000);
            var rare1 = TestClipFactory.CreateTone("Random_Rare1", 400f, 0.3f, weight: 0);
            var rare2 = TestClipFactory.CreateTone("Random_Rare2", 500f, 0.3f, weight: 0);
            var entity = TestAudioEntityFactory
                .Create("Random", BroAudioType.SFX, dominant, rare1, rare2)
                .WithPlayMode(MulticlipsPlayMode.Random);
            var id = new SoundID(entity);

            for (int i = 0; i < 3; i++)
            {
                IAudioPlayer player = BroAudio.Play(id);
                yield return null;
                Assert.AreSame(dominant, player.CurrentPlayingClip, "The overwhelmingly-weighted clip should always be picked.");
                player.Stop(0f);
                yield return null;
            }
        }
    }

    /// <summary>
    /// MulticlipsPlayMode.Shuffle eventually covers every clip. Note it's weaker than "no repeats within a
    /// round": ShuffleClipStrategy.Use() only rejects a pick that equals `_lastUsed` (the clip that ended the
    /// previous round), not one already in `_used` this round — so a fast-path pick can revisit an
    /// already-used clip mid-round; only the round *boundary* is guaranteed non-repeating. See
    /// Docs/KNOWN_BEHAVIOR_QUIRKS.md. Asserting "no repeats in exactly N plays" would be flaky, so this
    /// scenario instead asserts the true, non-flaky guarantee: variety shows up over enough plays.
    /// </summary>
    internal sealed class ShuffleClipSelectionScenario : IVerificationScenario
    {
        private const int PlayCount = 20;

        public string Description => "MulticlipsPlayMode.Shuffle picks from every clip over enough plays";
        public string ExpectedOutcome => "Across many plays, every distinct clip is picked at least once";

        public IEnumerator Run(VerificationContext context)
        {
            var clips = new[]
            {
                TestClipFactory.CreateTone("Shuffle_0", 300f, 0.3f),
                TestClipFactory.CreateTone("Shuffle_1", 400f, 0.3f),
                TestClipFactory.CreateTone("Shuffle_2", 500f, 0.3f),
            };
            var entity = TestAudioEntityFactory
                .Create("Shuffle", BroAudioType.SFX, clips)
                .WithPlayMode(MulticlipsPlayMode.Shuffle);
            var id = new SoundID(entity);

            var picked = new HashSet<IBroAudioClip>();
            for (int i = 0; i < PlayCount; i++)
            {
                IAudioPlayer player = BroAudio.Play(id);
                yield return null;
                picked.Add(player.CurrentPlayingClip);
                player.Stop(0f);
                yield return null;
            }

            Assert.AreEqual(clips.Length, picked.Count, $"All {clips.Length} clips should have appeared across {PlayCount} plays.");
        }
    }

    /// <summary>
    /// MulticlipsPlayMode.Velocity selects the clip whose Weight is the closest boundary at or below the
    /// given velocity: VelocityClipStrategy returns the clip *before* the first one whose Weight exceeds the
    /// value (or the last clip if none exceed it). See Runtime/Utility/ClipSelection/VelocityClipStrategy.cs.
    /// </summary>
    internal sealed class VelocityClipSelectionScenario : IVerificationScenario
    {
        public string Description => "MulticlipsPlayMode.Velocity picks the clip matching the given velocity band";
        public string ExpectedOutcome => "SetVelocity(30) picks the low layer, SetVelocity(70) the mid layer, SetVelocity(150) the top layer";

        public IEnumerator Run(VerificationContext context)
        {
            var low = TestClipFactory.CreateTone("Velocity_Low", 300f, 0.3f, weight: 0);
            var mid = TestClipFactory.CreateTone("Velocity_Mid", 400f, 0.3f, weight: 50);
            var high = TestClipFactory.CreateTone("Velocity_High", 500f, 0.3f, weight: 100);
            var entity = TestAudioEntityFactory
                .Create("Velocity", BroAudioType.SFX, low, mid, high)
                .WithPlayMode(MulticlipsPlayMode.Velocity);
            var id = new SoundID(entity);

            yield return PlayAtVelocity(id, 30, low, "a velocity below the mid boundary should pick the low layer");
            yield return PlayAtVelocity(id, 70, mid, "a velocity between the mid and high boundaries should pick the mid layer");
            yield return PlayAtVelocity(id, 150, high, "a velocity above every boundary should pick the last (highest) layer");
        }

        private static IEnumerator PlayAtVelocity(SoundID id, int velocity, IBroAudioClip expected, string because)
        {
            IAudioPlayer player = BroAudio.Play(id);
            player.SetVelocity(velocity); // Must be set before the queued Play() runs so PickNewClip sees it.
            yield return null;
            Assert.AreSame(expected, player.CurrentPlayingClip, because);
            player.Stop(0f);
            yield return null;
        }
    }

    /// <summary>
    /// MulticlipsPlayMode.Chained starts on the intro clip (PlaybackStage.Start, ClipSelectionContext.Value=1
    /// maps to index 0). The automatic intro-&gt;loop-&gt;outro handover is scoped out here: it depends on the
    /// intro clip's full playable duration elapsing, which is slow and timing-fragile for Layer 1's state-level
    /// suite. It's left as an explicit gap; see VERIFICATION_PLAN.md's exit criteria on waivers.
    /// </summary>
    internal sealed class ChainedClipSelectionScenario : IVerificationScenario
    {
        public string Description => "MulticlipsPlayMode.Chained starts playback on the intro clip";
        public string ExpectedOutcome => "The first play picks clips[0] (the Start stage clip)";

        public IEnumerator Run(VerificationContext context)
        {
            var intro = TestClipFactory.CreateTone("Chained_Intro", 300f, 0.3f);
            var loop = TestClipFactory.CreateTone("Chained_Loop", 400f, 0.3f);
            var outro = TestClipFactory.CreateTone("Chained_Outro", 500f, 0.3f);
            var entity = TestAudioEntityFactory
                .Create("Chained", BroAudioType.SFX, intro, loop, outro)
                .WithPlayMode(MulticlipsPlayMode.Chained);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;

            Assert.AreSame(intro, player.CurrentPlayingClip, "Chained playback should start on the intro (Start stage) clip.");

            // Not cleaning up with player.Stop(0f) here on purpose: Chained mode defaults to a loop
            // (DefaultChainedPlayModeLoop), so CanHandoverToEnd() is true at the Start stage and Stop()
            // would hand over to the outro clip instead of ending playback. The test teardown's blanket
            // Stop(BroAudioType.All) runs after ChainedModeStage has moved past Start, so it stops cleanly.
        }
    }
}
