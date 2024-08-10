// Auto-generated code
using System;
using UnityEngine;
using UnityEngine.Audio;
namespace Ami.Extension
{
    public class AudioSourceProxy : BroModifier<AudioSource>, IAudioSourceProxy
    {
        public AudioSourceProxy(AudioSource @base) : base(@base) {}

        public const float Default_Volume = default;
        private bool _hasVolumeResetAction = false;
        public float volume
        {
            set
            {
                AddResetAction(ref _hasVolumeResetAction, () => Base.volume = Default_Volume);
                Base.volume = value;
            }
        }

        public const float Default_Pitch = default;
        private bool _hasPitchResetAction = false;
        public float pitch
        {
            set
            {
                AddResetAction(ref _hasPitchResetAction, () => Base.pitch = Default_Pitch);
                Base.pitch = value;
            }
        }

        public const float Default_Time = default;
        private bool _hasTimeResetAction = false;
        public float time
        {
            set
            {
                AddResetAction(ref _hasTimeResetAction, () => Base.time = Default_Time);
                Base.time = value;
            }
        }

        public const int Default_TimeSamples = default;
        private bool _hasTimeSamplesResetAction = false;
        public int timeSamples
        {
            set
            {
                AddResetAction(ref _hasTimeSamplesResetAction, () => Base.timeSamples = Default_TimeSamples);
                Base.timeSamples = value;
            }
        }

        public const AudioClip Default_Clip = default;
        private bool _hasClipResetAction = false;
        public AudioClip clip
        {
            set
            {
                AddResetAction(ref _hasClipResetAction, () => Base.clip = Default_Clip);
                Base.clip = value;
            }
        }

        public const AudioMixerGroup Default_OutputAudioMixerGroup = default;
        private bool _hasOutputAudioMixerGroupResetAction = false;
        public AudioMixerGroup outputAudioMixerGroup
        {
            set
            {
                AddResetAction(ref _hasOutputAudioMixerGroupResetAction, () => Base.outputAudioMixerGroup = Default_OutputAudioMixerGroup);
                Base.outputAudioMixerGroup = value;
            }
        }

        public const GamepadSpeakerOutputType Default_GamepadSpeakerOutputType = default;
        private bool _hasGamepadSpeakerOutputTypeResetAction = false;
        public GamepadSpeakerOutputType gamepadSpeakerOutputType
        {
            set
            {
                AddResetAction(ref _hasGamepadSpeakerOutputTypeResetAction, () => Base.gamepadSpeakerOutputType = Default_GamepadSpeakerOutputType);
                Base.gamepadSpeakerOutputType = value;
            }
        }

        public const bool Default_Loop = default;
        private bool _hasLoopResetAction = false;
        public bool loop
        {
            set
            {
                AddResetAction(ref _hasLoopResetAction, () => Base.loop = Default_Loop);
                Base.loop = value;
            }
        }

        public const bool Default_IgnoreListenerVolume = default;
        private bool _hasIgnoreListenerVolumeResetAction = false;
        public bool ignoreListenerVolume
        {
            set
            {
                AddResetAction(ref _hasIgnoreListenerVolumeResetAction, () => Base.ignoreListenerVolume = Default_IgnoreListenerVolume);
                Base.ignoreListenerVolume = value;
            }
        }

        public const bool Default_PlayOnAwake = default;
        private bool _hasPlayOnAwakeResetAction = false;
        public bool playOnAwake
        {
            set
            {
                AddResetAction(ref _hasPlayOnAwakeResetAction, () => Base.playOnAwake = Default_PlayOnAwake);
                Base.playOnAwake = value;
            }
        }

        public const bool Default_IgnoreListenerPause = default;
        private bool _hasIgnoreListenerPauseResetAction = false;
        public bool ignoreListenerPause
        {
            set
            {
                AddResetAction(ref _hasIgnoreListenerPauseResetAction, () => Base.ignoreListenerPause = Default_IgnoreListenerPause);
                Base.ignoreListenerPause = value;
            }
        }

        public const AudioVelocityUpdateMode Default_VelocityUpdateMode = default;
        private bool _hasVelocityUpdateModeResetAction = false;
        public AudioVelocityUpdateMode velocityUpdateMode
        {
            set
            {
                AddResetAction(ref _hasVelocityUpdateModeResetAction, () => Base.velocityUpdateMode = Default_VelocityUpdateMode);
                Base.velocityUpdateMode = value;
            }
        }

        public const float Default_PanStereo = default;
        private bool _hasPanStereoResetAction = false;
        public float panStereo
        {
            set
            {
                AddResetAction(ref _hasPanStereoResetAction, () => Base.panStereo = Default_PanStereo);
                Base.panStereo = value;
            }
        }

        public const float Default_SpatialBlend = default;
        private bool _hasSpatialBlendResetAction = false;
        public float spatialBlend
        {
            set
            {
                AddResetAction(ref _hasSpatialBlendResetAction, () => Base.spatialBlend = Default_SpatialBlend);
                Base.spatialBlend = value;
            }
        }

        public const bool Default_Spatialize = default;
        private bool _hasSpatializeResetAction = false;
        public bool spatialize
        {
            set
            {
                AddResetAction(ref _hasSpatializeResetAction, () => Base.spatialize = Default_Spatialize);
                Base.spatialize = value;
            }
        }

        public const bool Default_SpatializePostEffects = default;
        private bool _hasSpatializePostEffectsResetAction = false;
        public bool spatializePostEffects
        {
            set
            {
                AddResetAction(ref _hasSpatializePostEffectsResetAction, () => Base.spatializePostEffects = Default_SpatializePostEffects);
                Base.spatializePostEffects = value;
            }
        }

        public const float Default_ReverbZoneMix = default;
        private bool _hasReverbZoneMixResetAction = false;
        public float reverbZoneMix
        {
            set
            {
                AddResetAction(ref _hasReverbZoneMixResetAction, () => Base.reverbZoneMix = Default_ReverbZoneMix);
                Base.reverbZoneMix = value;
            }
        }

        public const bool Default_BypassEffects = default;
        private bool _hasBypassEffectsResetAction = false;
        public bool bypassEffects
        {
            set
            {
                AddResetAction(ref _hasBypassEffectsResetAction, () => Base.bypassEffects = Default_BypassEffects);
                Base.bypassEffects = value;
            }
        }

        public const bool Default_BypassListenerEffects = default;
        private bool _hasBypassListenerEffectsResetAction = false;
        public bool bypassListenerEffects
        {
            set
            {
                AddResetAction(ref _hasBypassListenerEffectsResetAction, () => Base.bypassListenerEffects = Default_BypassListenerEffects);
                Base.bypassListenerEffects = value;
            }
        }

        public const bool Default_BypassReverbZones = default;
        private bool _hasBypassReverbZonesResetAction = false;
        public bool bypassReverbZones
        {
            set
            {
                AddResetAction(ref _hasBypassReverbZonesResetAction, () => Base.bypassReverbZones = Default_BypassReverbZones);
                Base.bypassReverbZones = value;
            }
        }

        public const float Default_DopplerLevel = default;
        private bool _hasDopplerLevelResetAction = false;
        public float dopplerLevel
        {
            set
            {
                AddResetAction(ref _hasDopplerLevelResetAction, () => Base.dopplerLevel = Default_DopplerLevel);
                Base.dopplerLevel = value;
            }
        }

        public const float Default_Spread = default;
        private bool _hasSpreadResetAction = false;
        public float spread
        {
            set
            {
                AddResetAction(ref _hasSpreadResetAction, () => Base.spread = Default_Spread);
                Base.spread = value;
            }
        }

        public const int Default_Priority = default;
        private bool _hasPriorityResetAction = false;
        public int priority
        {
            set
            {
                AddResetAction(ref _hasPriorityResetAction, () => Base.priority = Default_Priority);
                Base.priority = value;
            }
        }

        public const bool Default_Mute = default;
        private bool _hasMuteResetAction = false;
        public bool mute
        {
            set
            {
                AddResetAction(ref _hasMuteResetAction, () => Base.mute = Default_Mute);
                Base.mute = value;
            }
        }

        public const float Default_MinDistance = default;
        private bool _hasMinDistanceResetAction = false;
        public float minDistance
        {
            set
            {
                AddResetAction(ref _hasMinDistanceResetAction, () => Base.minDistance = Default_MinDistance);
                Base.minDistance = value;
            }
        }

        public const float Default_MaxDistance = default;
        private bool _hasMaxDistanceResetAction = false;
        public float maxDistance
        {
            set
            {
                AddResetAction(ref _hasMaxDistanceResetAction, () => Base.maxDistance = Default_MaxDistance);
                Base.maxDistance = value;
            }
        }

        public const AudioRolloffMode Default_RolloffMode = default;
        private bool _hasRolloffModeResetAction = false;
        public AudioRolloffMode rolloffMode
        {
            set
            {
                AddResetAction(ref _hasRolloffModeResetAction, () => Base.rolloffMode = Default_RolloffMode);
                Base.rolloffMode = value;
            }
        }

    }
}
