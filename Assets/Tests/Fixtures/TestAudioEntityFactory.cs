using Ami.BroAudio;
using Ami.BroAudio.Data;
using Ami.Extension;

namespace Ami.BroAudio.Tests.Fixtures
{
    /// <summary>
    /// Builds <see cref="AudioEntity"/> instances entirely in code via <see cref="AudioEntity.CreateNewInstance"/>,
    /// as prescribed by VERIFICATION_PLAN.md's Fixtures section (repo hooks block hand-editing .asset YAML).
    /// Entities are created without an <see cref="AudioAsset"/>, so <c>entity.PlaybackGroup</c> resolves to null
    /// (see AudioEntity._upperGroup) and playback bypasses PlaybackGroup validation entirely unless
    /// <see cref="WithPlaybackGroup"/> is used to opt a scenario into group rules on purpose.
    /// </summary>
    internal static class TestAudioEntityFactory
    {
        internal static AudioEntity Create(string name, BroAudioType audioType, params BroAudioClip[] clips)
        {
            AudioEntity entity = AudioEntity.CreateNewInstance(asset: null, name, audioType);
            entity.Clips = clips;
            return entity;
        }

        internal static AudioEntity WithPlayMode(this AudioEntity entity, MulticlipsPlayMode mode)
        {
            ReflectionUtility.SetField(entity, "MulticlipsPlayMode", mode);
            return entity;
        }

        internal static AudioEntity WithLoop(this AudioEntity entity, bool loop = true)
        {
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.Loop), loop);
            return entity;
        }

        internal static AudioEntity WithSeamlessLoop(this AudioEntity entity, float transitionTime)
        {
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.SeamlessLoop), true);
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.TransitionTime), transitionTime);
            return entity;
        }

        internal static AudioEntity WithRandomFlags(this AudioEntity entity, RandomFlag flags, float pitchRandomRange = 0f, float volumeRandomRange = 0f)
        {
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.RandomFlags), flags);
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.PitchRandomRange), pitchRandomRange);
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.VolumeRandomRange), volumeRandomRange);
            return entity;
        }

        internal static AudioEntity WithEntityFlags(this AudioEntity entity, AudioEntityFlag flags)
        {
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.Flags), flags);
            return entity;
        }

        internal static AudioEntity WithPlaybackGroup(this AudioEntity entity, PlaybackGroup group)
        {
            ReflectionUtility.SetField(entity, "_group", group);
            return entity;
        }

        internal static AudioEntity WithPitch(this AudioEntity entity, float pitch)
        {
            ReflectionUtility.SetProperty(entity, nameof(AudioEntity.Pitch), pitch);
            return entity;
        }
    }
}
