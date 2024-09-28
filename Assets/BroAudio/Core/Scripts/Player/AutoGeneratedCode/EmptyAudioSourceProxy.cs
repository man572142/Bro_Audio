// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public class EmptyAudioSourceProxy : IAudioSourceProxy
    {
        public float volume { get => 1; set { } }

        public float pitch { get => 1; set { } }

        public float time { get => 0; set { } }

        public int timeSamples { get => 0; set { } }

        public AudioClip clip { get => default; set { } }

        public AudioMixerGroup outputAudioMixerGroup { get => default; set { } }

#if (UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5
        public GamepadSpeakerOutputType gamepadSpeakerOutputType { get => UnityEngine.GamepadSpeakerOutputType.Speaker; set { } }
#endif

        public bool loop { get => false; set { } }

        public bool ignoreListenerVolume { get => false; set { } }

        public bool playOnAwake { get => true; set { } }

        public bool ignoreListenerPause { get => false; set { } }

        public AudioVelocityUpdateMode velocityUpdateMode { get => UnityEngine.AudioVelocityUpdateMode.Auto; set { } }

        public float panStereo { get => 0; set { } }

        public float spatialBlend { get => 0; set { } }

        public bool spatialize { get => false; set { } }

        public bool spatializePostEffects { get => false; set { } }

        public float reverbZoneMix { get => 1; set { } }

        public bool bypassEffects { get => false; set { } }

        public bool bypassListenerEffects { get => false; set { } }

        public bool bypassReverbZones { get => false; set { } }

        public float dopplerLevel { get => 1; set { } }

        public float spread { get => 0; set { } }

        public int priority { get => 128; set { } }

        public bool mute { get => false; set { } }

        public float minDistance { get => 1; set { } }

        public float maxDistance { get => 500; set { } }

        public AudioRolloffMode rolloffMode { get => UnityEngine.AudioRolloffMode.Logarithmic; set { } }

        public AnimationCurve GetCustomCurve(AudioSourceCurveType type) { return default; }

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve) {  }

        public bool GetAmbisonicDecoderFloat(int index, out float value) { value = default; return default; }

        public bool SetAmbisonicDecoderFloat(int index, float value) { return default; }

        public bool GetSpatializerFloat(int index, out float value) { value = default; return default; }

        public bool SetSpatializerFloat(int index, float value) { return default; }

    }
}
