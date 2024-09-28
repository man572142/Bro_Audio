// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public interface IAudioSourceProxy
    {
        /// <inheritdoc cref="AudioSource.volume"/>
        float volume { get; set; }

        /// <inheritdoc cref="AudioSource.pitch"/>
        float pitch { get; set; }

        /// <inheritdoc cref="AudioSource.time"/>
        float time { get; set; }

        /// <inheritdoc cref="AudioSource.timeSamples"/>
        int timeSamples { get; set; }

        /// <inheritdoc cref="AudioSource.clip"/>
        AudioClip clip { get; set; }

        /// <inheritdoc cref="AudioSource.outputAudioMixerGroup"/>
        AudioMixerGroup outputAudioMixerGroup { get; set; }

#if (UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5
        /// <inheritdoc cref="AudioSource.gamepadSpeakerOutputType"/>
        GamepadSpeakerOutputType gamepadSpeakerOutputType { get; set; }
#endif

        /// <inheritdoc cref="AudioSource.loop"/>
        bool loop { get; set; }

        /// <inheritdoc cref="AudioSource.ignoreListenerVolume"/>
        bool ignoreListenerVolume { get; set; }

        /// <inheritdoc cref="AudioSource.playOnAwake"/>
        bool playOnAwake { get; set; }

        /// <inheritdoc cref="AudioSource.ignoreListenerPause"/>
        bool ignoreListenerPause { get; set; }

        /// <inheritdoc cref="AudioSource.velocityUpdateMode"/>
        AudioVelocityUpdateMode velocityUpdateMode { get; set; }

        /// <inheritdoc cref="AudioSource.panStereo"/>
        float panStereo { get; set; }

        /// <inheritdoc cref="AudioSource.spatialBlend"/>
        float spatialBlend { get; set; }

        /// <inheritdoc cref="AudioSource.spatialize"/>
        bool spatialize { get; set; }

        /// <inheritdoc cref="AudioSource.spatializePostEffects"/>
        bool spatializePostEffects { get; set; }

        /// <inheritdoc cref="AudioSource.reverbZoneMix"/>
        float reverbZoneMix { get; set; }

        /// <inheritdoc cref="AudioSource.bypassEffects"/>
        bool bypassEffects { get; set; }

        /// <inheritdoc cref="AudioSource.bypassListenerEffects"/>
        bool bypassListenerEffects { get; set; }

        /// <inheritdoc cref="AudioSource.bypassReverbZones"/>
        bool bypassReverbZones { get; set; }

        /// <inheritdoc cref="AudioSource.dopplerLevel"/>
        float dopplerLevel { get; set; }

        /// <inheritdoc cref="AudioSource.spread"/>
        float spread { get; set; }

        /// <inheritdoc cref="AudioSource.priority"/>
        int priority { get; set; }

        /// <inheritdoc cref="AudioSource.mute"/>
        bool mute { get; set; }

        /// <inheritdoc cref="AudioSource.minDistance"/>
        float minDistance { get; set; }

        /// <inheritdoc cref="AudioSource.maxDistance"/>
        float maxDistance { get; set; }

        /// <inheritdoc cref="AudioSource.rolloffMode"/>
        AudioRolloffMode rolloffMode { get; set; }

        /// <inheritdoc cref="AudioSource.GetCustomCurve"/>
        AnimationCurve GetCustomCurve(AudioSourceCurveType type);

        /// <inheritdoc cref="AudioSource.SetCustomCurve"/>
        void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve);

        /// <inheritdoc cref="AudioSource.GetAmbisonicDecoderFloat"/>
        bool GetAmbisonicDecoderFloat(int index, out float value);

        /// <inheritdoc cref="AudioSource.SetAmbisonicDecoderFloat"/>
        bool SetAmbisonicDecoderFloat(int index, float value);

        /// <inheritdoc cref="AudioSource.GetSpatializerFloat"/>
        bool GetSpatializerFloat(int index, out float value);

        /// <inheritdoc cref="AudioSource.SetSpatializerFloat"/>
        bool SetSpatializerFloat(int index, float value);

    }
}
