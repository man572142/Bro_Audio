using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.MonoComponents
{
    /// <summary>
    /// SoundSource's `_sound` field is a private [SerializeField] meant to be authored in the Inspector, so
    /// (like TestAudioEntityFactory does for AudioEntity) this scenario has to reach it via
    /// ReflectionUtility.SetField instead of a public setter. PositionMode is left at its default (Global),
    /// which routes Play() through PlayGlobally() -> BroAudio.Play(SoundID, PlaybackGroup) — the plainest of
    /// the three Play() branches, and enough to characterize CurrentPlayer bookkeeping and Stop() without
    /// pulling in position/follow-target concerns already covered elsewhere. The `_playOnEnable` /
    /// `_stopOnDisable` / `_onlyPlayOnce` auto-behavior toggles are deliberately not exercised here (see
    /// scoped-down note in the task's final report) — this only characterizes the manual Play()/Stop() API.
    /// </summary>
    internal sealed class SoundSourcePlaysAndStopsScenario : IVerificationScenario
    {
        public string Description => "SoundSource.Play() creates and tracks a player through CurrentPlayer, and Stop() releases it";
        public string ExpectedOutcome => "After Play(), IsPlaying is true and CurrentPlayer reflects a playing IAudioPlayer; after Stop(0f), IsPlaying is false";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("SoundSource_Tone", 300f, 1f);
            var entity = TestAudioEntityFactory.Create("SoundSource", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            var go = new GameObject("VerificationSoundSource");
            var soundSource = go.AddComponent<SoundSource>();
            ReflectionUtility.SetField(soundSource, "_sound", id);

            try
            {
                soundSource.Play();
                yield return null;

                Assert.IsTrue(soundSource.IsPlaying, "SoundSource should report IsPlaying after Play().");
                Assert.IsNotNull(soundSource.CurrentPlayer, "CurrentPlayer should be set to the player created by Play().");
                Assert.IsTrue(soundSource.CurrentPlayer.IsPlaying, "The player tracked by CurrentPlayer should itself be playing.");

                soundSource.Stop(0f);
                yield return null;

                Assert.IsFalse(soundSource.IsPlaying, "SoundSource should no longer report IsPlaying after Stop(0f).");
            }
            finally
            {
                Object.Destroy(go);
            }
        }
    }
}
