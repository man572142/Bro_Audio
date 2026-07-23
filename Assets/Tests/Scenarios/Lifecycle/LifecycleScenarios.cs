using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;

namespace Ami.BroAudio.Tests.Scenarios.Lifecycle
{
    /// <summary>Several concurrent plays should all return to an idle state once stopped — no leaked player.</summary>
    internal sealed class PoolIntegrityScenario : IVerificationScenario
    {
        private const int ConcurrentPlays = 5;

        public string Description => "N concurrent plays all return to an idle state after stopping";
        public string ExpectedOutcome => "Every player reports inactive, and HasAnyPlayingInstances is false for every id, once all are stopped";

        public IEnumerator Run(VerificationContext context)
        {
            var ids = new SoundID[ConcurrentPlays];
            var players = new IAudioPlayer[ConcurrentPlays];

            for (int i = 0; i < ConcurrentPlays; i++)
            {
                var tone = TestClipFactory.CreateTone($"Pool_Tone_{i}", 300f + i * 20f, 2f);
                var entity = TestAudioEntityFactory.Create($"Pool_{i}", BroAudioType.SFX, tone);
                ids[i] = new SoundID(entity);
                players[i] = BroAudio.Play(ids[i]);
            }
            yield return null;

            for (int i = 0; i < ConcurrentPlays; i++)
            {
                Assert.IsTrue(players[i].IsPlaying, $"Player {i} should be playing.");
                Assert.IsTrue(BroAudio.HasAnyPlayingInstances(ids[i]));
            }

            for (int i = 0; i < ConcurrentPlays; i++)
            {
                players[i].Stop(0f);
            }
            yield return null;

            for (int i = 0; i < ConcurrentPlays; i++)
            {
                Assert.IsFalse(players[i].IsActive, $"Player {i} should have been recycled back to the pool.");
                Assert.IsFalse(BroAudio.HasAnyPlayingInstances(ids[i]), $"Id {i} shouldn't report as playing after stop.");
            }
        }
    }

    /// <summary>HasAnyPlayingInstances and TryGetEntityInfo reflect live playback state.</summary>
    internal sealed class QueriesScenario : IVerificationScenario
    {
        public string Description => "HasAnyPlayingInstances and TryGetEntityInfo reflect live playback state";
        public string ExpectedOutcome => "Both queries are false/empty before playing, true/populated while playing, and false again after stopping";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("Queries_Tone", 310f, 1f);
            var entity = TestAudioEntityFactory.Create("Queries", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            Assert.IsFalse(BroAudio.HasAnyPlayingInstances(id), "Nothing has been played yet.");

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;

            Assert.IsTrue(BroAudio.HasAnyPlayingInstances(id));
            Assert.IsTrue(BroAudio.TryGetEntityInfo(id, out var entityInfo));
            Assert.AreEqual(1, entityInfo.Clips.Count);

            player.Stop(0f);
            yield return null;

            Assert.IsFalse(BroAudio.HasAnyPlayingInstances(id), "Should no longer report as playing once stopped.");
        }
    }
}
