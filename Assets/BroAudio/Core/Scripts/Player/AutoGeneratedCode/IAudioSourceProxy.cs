// Auto-generated code
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public interface IAudioSourceProxy
    {
        float volume { set; }
        float pitch { set; }
        float time { set; }
        int timeSamples { set; }
        AudioClip clip { set; }
        AudioMixerGroup outputAudioMixerGroup { set; }
        GamepadSpeakerOutputType gamepadSpeakerOutputType { set; }
        bool loop { set; }
        bool ignoreListenerVolume { set; }
        bool playOnAwake { set; }
        bool ignoreListenerPause { set; }
        AudioVelocityUpdateMode velocityUpdateMode { set; }
        float panStereo { set; }
        float spatialBlend { set; }
        bool spatialize { set; }
        bool spatializePostEffects { set; }
        float reverbZoneMix { set; }
        bool bypassEffects { set; }
        bool bypassListenerEffects { set; }
        bool bypassReverbZones { set; }
        float dopplerLevel { set; }
        float spread { set; }
        int priority { set; }
        bool mute { set; }
        float minDistance { set; }
        float maxDistance { set; }
        AudioRolloffMode rolloffMode { set; }
    }
}
