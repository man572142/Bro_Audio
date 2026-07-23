using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using Ami.Extension;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Volume
{
    /// <summary>BroAudio.SetVolume(id, ...) changes the player's composite volume, immediately or over a fade.</summary>
    internal sealed class SetVolumeScenario : IVerificationScenario
    {
        public string Description => "SetVolume(id, vol[, fadeTime]) changes a playing sound's volume";
        public string ExpectedOutcome => "GetVolume() snaps immediately with no fade time, and ramps smoothly otherwise";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("SetVolume_Tone", 320f, 2f);
            var entity = TestAudioEntityFactory.Create("SetVolume", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;
            Assert.AreEqual(1f, player.GetVolume(), 0.01f);

            BroAudio.SetVolume(id, 0.3f);
            yield return null;
            Assert.AreEqual(0.3f, player.GetVolume(), 0.01f, "An immediate SetVolume should snap the composite volume.");

            BroAudio.SetVolume(id, 0.8f, 0.3f);
            yield return new WaitForSeconds(0.15f);
            float midVolume = player.GetVolume();
            Assert.Greater(midVolume, 0.3f);
            Assert.Less(midVolume, 0.8f);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0.8f, player.GetVolume(), 0.02f, "The volume fade should settle at the target.");

            player.Stop(0f);
        }
    }

    /// <summary>BroAudio.SetPitch(id, ...) changes AudioSource.pitch, immediately or over a fade.</summary>
    internal sealed class SetPitchScenario : IVerificationScenario
    {
        public string Description => "SetPitch(id, pitch[, fadeTime]) changes a playing sound's pitch";
        public string ExpectedOutcome => "AudioSource.pitch snaps immediately with no fade time, and ramps smoothly otherwise";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("SetPitch_Tone", 300f, 2f);
            var entity = TestAudioEntityFactory.Create("SetPitch", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null;
            Assert.AreEqual(1f, player.AudioSource.pitch, 0.001f);

            BroAudio.SetPitch(id, 1.5f);
            yield return null;
            Assert.AreEqual(1.5f, player.AudioSource.pitch, 0.001f, "An immediate SetPitch should snap the source pitch.");

            BroAudio.SetPitch(id, 0.5f, 0.3f);
            yield return new WaitForSeconds(0.15f);
            float midPitch = player.AudioSource.pitch;
            Assert.Less(midPitch, 1.5f);
            Assert.Greater(midPitch, 0.5f);

            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0.5f, player.AudioSource.pitch, 0.05f, "The pitch fade should settle at the target.");

            player.Stop(0f);
        }
    }

    /// <summary>
    /// Regression pin for 529b085: an explicit SetPitch(1) must win over the entity's pitch randomization,
    /// even with a wide random range that would otherwise make landing on exactly 1 vanishingly unlikely.
    /// </summary>
    internal sealed class ExplicitPitchOverridesRandomizationScenario : IVerificationScenario
    {
        public string Description => "Explicit SetPitch(1) overrides the entity's pitch randomization";
        public string ExpectedOutcome => "AudioSource.pitch is exactly 1 despite a wide pitch-random range on the entity";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PitchOverride_Tone", 300f, 1f);
            var entity = TestAudioEntityFactory
                .Create("PitchOverride", BroAudioType.SFX, tone)
                .WithPitch(1f)
                .WithRandomFlags(RandomFlag.Pitch, pitchRandomRange: 2f);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            player.SetPitch(1f); // Set before the queued Play() runs this frame, so SetInitialPitch already sees TargetPitch.
            yield return null;

            Assert.AreEqual(1f, player.AudioSource.pitch, 0.0001f, "An explicit pitch of 1 must win over randomization.");

            player.Stop(0f);
        }
    }
}
