using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Tests.Fixtures
{
    /// <summary>
    /// Builds procedural sine-tone clips instead of committing wavs. Each clip is identifiable by its
    /// frequency, which is what makes clip-selection sequences assertable (see VERIFICATION_PLAN.md Fixtures).
    /// </summary>
    internal static class TestClipFactory
    {
        internal const int SampleRate = 44100;

        internal static AudioClip CreateToneClip(string name, float frequencyHz, float durationSeconds = 1f)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                data[i] = Mathf.Sin(2f * Mathf.PI * frequencyHz * i / SampleRate) * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        internal static BroAudioClip Wrap(AudioClip audioClip, float volume = 1f, float delay = 0f, int weight = 0, float fadeIn = 0f, float fadeOut = 0f)
        {
            var broClip = new BroAudioClip
            {
                Volume = volume,
                Delay = delay,
                Weight = weight,
                FadeIn = fadeIn,
                FadeOut = fadeOut,
            };
            ReflectionUtility.SetField(broClip, BroAudioClip.NameOf.AudioClip, audioClip);
            return broClip;
        }

        /// <summary>Creates a one-second tone clip wrapped as a BroAudioClip in one step.</summary>
        internal static BroAudioClip CreateTone(string name, float frequencyHz, float durationSeconds = 1f, float volume = 1f, float delay = 0f, int weight = 0)
        {
            return Wrap(CreateToneClip(name, frequencyHz, durationSeconds), volume, delay, weight);
        }
    }
}
