// Auto-generated code
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension.Reflection
{
    public interface IAudioSourceProxy
{
        Single panLevel { set; }
        Single pan { set; }
        Single volume { set; }
        Single pitch { set; }
        Single time { set; }
        Int32 timeSamples { set; }
        AudioClip clip { set; }
        AudioMixerGroup outputAudioMixerGroup { set; }
        GamepadSpeakerOutputType gamepadSpeakerOutputType { set; }
        Boolean isPlaying { set; }
        Boolean isVirtual { set; }
        Boolean loop { set; }
        Boolean ignoreListenerVolume { set; }
        Boolean playOnAwake { set; }
        Boolean ignoreListenerPause { set; }
        AudioVelocityUpdateMode velocityUpdateMode { set; }
        Single panStereo { set; }
        Single spatialBlend { set; }
        Boolean spatialize { set; }
        Boolean spatializePostEffects { set; }
        Single reverbZoneMix { set; }
        Boolean bypassEffects { set; }
        Boolean bypassListenerEffects { set; }
        Boolean bypassReverbZones { set; }
        Single dopplerLevel { set; }
        Single spread { set; }
        Int32 priority { set; }
        Boolean mute { set; }
        Single minDistance { set; }
        Single maxDistance { set; }
        AudioRolloffMode rolloffMode { set; }
        Single minVolume { set; }
        Single maxVolume { set; }
        Single rolloffFactor { set; }
}
}
