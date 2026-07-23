using System.Collections;
using Ami.BroAudio.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;

namespace Ami.BroAudio.Tests.Scenarios.Playback
{
    /// <summary>Play(id): the clip is assigned, the source starts playing, and the composite volume reaches full.</summary>
    internal sealed class PlayBasicScenario : IVerificationScenario
    {
        public string Description => "Play(id) starts playback of the entity's clip";
        public string ExpectedOutcome => "AudioSource.clip matches the picked clip, IsPlaying is true, GetVolume() reaches full volume";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PlayBasic_Tone", 440f, 1f);
            var entity = TestAudioEntityFactory.Create("PlayBasic", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id);
            yield return null; // SoundManager.LateUpdate dequeues and starts the player this frame.

            Assert.IsTrue(player.IsActive, "Player should be active immediately after Play() is called.");
            Assert.IsTrue(player.IsPlaying, "Player should be playing once SoundManager's queue has drained.");
            Assert.AreEqual(id, player.ID);
            Assert.AreSame(tone.GetAudioClip(), player.AudioSource.clip, "The single-clip strategy should pick the only clip.");
            Assert.AreEqual(1f, player.GetVolume(), 0.01f, "No fade in was requested, so volume should already be full.");

            player.Stop(0f);
            yield return null;
        }
    }

    /// <summary>Play(id, fadeIn): GetVolume() ramps up from ~0 to full over the requested duration.</summary>
    internal sealed class PlayFadeInScenario : IVerificationScenario
    {
        private const float FadeInSeconds = 0.4f;

        public string Description => "Play(id, fadeIn) ramps the composite volume up over the fade duration";
        public string ExpectedOutcome => "GetVolume() starts near 0, increases monotonically, and settles at full volume";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PlayFadeIn_Tone", 300f, 2f);
            var entity = TestAudioEntityFactory.Create("PlayFadeIn", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            IAudioPlayer player = BroAudio.Play(id, FadeInSeconds);
            yield return null;

            float volumeAtStart = player.GetVolume();
            yield return new WaitForSeconds(FadeInSeconds * 0.5f);
            float volumeMidFade = player.GetVolume();
            yield return new WaitForSeconds(FadeInSeconds + 0.1f);
            float volumeAfterFade = player.GetVolume();

            Assert.Less(volumeAtStart, 0.2f, "Volume should start near zero when a fade-in is requested.");
            Assert.Greater(volumeMidFade, volumeAtStart, "Volume should have risen partway through the fade.");
            Assert.Less(volumeMidFade, 0.95f, "Volume shouldn't have already reached full mid-fade.");
            Assert.AreEqual(1f, volumeAfterFade, 0.05f, "Volume should settle at full once the fade duration has elapsed.");

            player.Stop(0f);
            yield return null;
        }
    }

    /// <summary>Play(id, position): the source is forced to 3D and placed at the given world position.</summary>
    internal sealed class PlaySpatialScenario : IVerificationScenario
    {
        public string Description => "Play(id, position) plays the clip in 3D space at the given position";
        public string ExpectedOutcome => "The player's transform sits at the given position and spatialBlend is 3D";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PlaySpatial_Tone", 350f, 1f);
            var entity = TestAudioEntityFactory.Create("PlaySpatial", BroAudioType.SFX, tone);
            var id = new SoundID(entity);
            var position = new Vector3(3f, 0f, -2f);

            IAudioPlayer player = BroAudio.Play(id, position);
            yield return null;

            Assert.IsTrue(player.IsPlaying);
            Assert.AreEqual(1f, player.AudioSource.spatialBlend, 0.001f, "A positioned play should force the source to 3D.");

            player.Stop(0f);
            yield return null;
        }
    }

    /// <summary>
    /// Play(id, followTarget): forces 3D spatialization, and survives the target being destroyed mid-playback.
    /// IAudioPlayer doesn't expose the player's own Transform, so the position-tracking itself isn't
    /// state-assertable from the public contract — only its 3D-forcing side effect and its failure mode are.
    /// </summary>
    internal sealed class PlayFollowTargetScenario : IVerificationScenario
    {
        public string Description => "Play(id, followTarget) attaches playback to a transform and tolerates its destruction";
        public string ExpectedOutcome => "spatialBlend is forced to 3D, and playback continues uninterrupted after the target is destroyed";

        public IEnumerator Run(VerificationContext context)
        {
            var tone = TestClipFactory.CreateTone("PlayFollow_Tone", 360f, 2f);
            var entity = TestAudioEntityFactory.Create("PlayFollow", BroAudioType.SFX, tone);
            var id = new SoundID(entity);

            var followTarget = new GameObject("VerificationFollowTarget").transform;
            followTarget.position = Vector3.zero;

            IAudioPlayer player = BroAudio.Play(id, followTarget);
            yield return null;

            Assert.AreEqual(1f, player.AudioSource.spatialBlend, 0.001f, "A followed play should force the source to 3D.");

            Object.Destroy(followTarget.gameObject);
            yield return null; // AudioPlayer.Update() detects the destroyed target and stops re-parenting, without erroring.

            Assert.IsTrue(player.IsPlaying, "Losing the follow target shouldn't interrupt playback.");

            player.Stop(0f);
            yield return null;
        }
    }
}
