using System;
using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tests.Fixtures;
using Ami.BroAudio.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Decorators
{
    /// <summary>
    /// AsBGM().SetTransition(Transition.Immediate) stops the previous BGM synchronously and starts the new
    /// one, firing BroAudio.OnBGMChanged for each. Immediate is chosen specifically because it resolves
    /// every step (stop-previous, start-new) without any fade wait, so the whole transition completes within
    /// a single dequeued frame instead of depending on fade-duration timing.
    /// </summary>
    internal sealed class BgmTransitionScenario : IVerificationScenario
    {
        public string Description => "AsBGM().SetTransition(Immediate) swaps the current BGM and raises OnBGMChanged";
        public string ExpectedOutcome => "The previous BGM player is stopped, the new one plays, and OnBGMChanged fires once per BGM";

        public IEnumerator Run(VerificationContext context)
        {
            var toneA = TestClipFactory.CreateTone("BgmA_Tone", 260f, 2f);
            var entityA = TestAudioEntityFactory.Create("BgmA", BroAudioType.Music, toneA);
            var idA = new SoundID(entityA);

            var toneB = TestClipFactory.CreateTone("BgmB_Tone", 320f, 2f);
            var entityB = TestAudioEntityFactory.Create("BgmB", BroAudioType.Music, toneB);
            var idB = new SoundID(entityB);

            var changedTo = new List<IAudioPlayer>();
            Action<IAudioPlayer> onChanged = p => changedTo.Add(p);
            BroAudio.OnBGMChanged += onChanged;

            try
            {
                IAudioPlayer playerA = BroAudio.Play(idA).AsBGM().SetTransition(Transition.Immediate);
                yield return null;

                Assert.IsTrue(playerA.IsPlaying);
                Assert.AreEqual(1, changedTo.Count, "The first BGM should raise OnBGMChanged once.");
                Assert.AreEqual(idA, changedTo[0].ID);

                IAudioPlayer playerB = BroAudio.Play(idB).AsBGM().SetTransition(Transition.Immediate);
                yield return null;

                Assert.IsFalse(playerA.IsActive, "An Immediate transition should stop the previous BGM synchronously.");
                Assert.IsTrue(playerB.IsPlaying, "The new BGM should be playing.");
                Assert.AreEqual(2, changedTo.Count, "The second BGM should raise OnBGMChanged again.");
                Assert.AreEqual(idB, changedTo[1].ID);

                playerB.Stop(0f);
                yield return null;
            }
            finally
            {
                BroAudio.OnBGMChanged -= onChanged;
            }
        }
    }

    /// <summary>
    /// AsDominator().QuietOthers ducks the shared "Main_Dominated" mixer bus that non-dominator players route
    /// through while a dominator is active (see EffectAutomationHelper/SwitchMainTrackMode), and restores it
    /// once the dominator stops (the QuietOthers(..., fadeTime: 0) call auto-resets via an IAutoResetWaitable
    /// tied to the dominator's own IsActive state — not to any individual other player's mixer group).
    /// </summary>
    internal sealed class DominatorQuietOthersScenario : IVerificationScenario
    {
        public string Description => "AsDominator().QuietOthers ducks and later restores the shared dominated-track volume";
        public string ExpectedOutcome => "Main_Dominated's mixer volume drops while the dominator plays, and returns to baseline once it stops";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("DominatorMain_Tone", 240f, 2f);
            var entity = TestAudioEntityFactory.Create("DominatorMain", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            SoundManager.Instance.AudioMixer.GetFloat(BroName.MainDominatedTrackName, out float baselineDb);

            IAudioPlayer dominator = BroAudio.Play(id).AsDominator().QuietOthers(0.25f, 0f);
            yield return null;

            SoundManager.Instance.AudioMixer.GetFloat(BroName.MainDominatedTrackName, out float duckedDb);
            Assert.Less(duckedDb, baselineDb, "QuietOthers should duck the shared dominated-track volume.");

            dominator.Stop(0f);
            // The auto-reset waitable is a WaitWhile polled once per frame by Unity's coroutine scheduler,
            // so give it a couple of frames to notice IsActive flip false and tween back to baseline.
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.1f);

            SoundManager.Instance.AudioMixer.GetFloat(BroName.MainDominatedTrackName, out float restoredDb);
            Assert.AreEqual(baselineDb, restoredDb, 0.5f, "The dominated-track volume should restore once the dominator stops.");
        }
    }
}
