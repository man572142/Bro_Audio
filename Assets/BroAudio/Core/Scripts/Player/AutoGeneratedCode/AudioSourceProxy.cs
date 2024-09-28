// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : BroModifier<AudioSource>, IAudioSourceProxy
    {
        public AudioSourceProxy(AudioSource @base) : base(@base) {}

        private bool _hasVolumeResetAction = false;
        public float volume
        {
            get => Base.volume;
            set
            {
                AddResetAction(ref _hasVolumeResetAction, () => Base.volume = 1);
                Base.volume = value;
            }
        }

        private bool _hasPitchResetAction = false;
        public float pitch
        {
            get => Base.pitch;
            set
            {
                AddResetAction(ref _hasPitchResetAction, () => Base.pitch = 1);
                Base.pitch = value;
            }
        }

        private bool _hasTimeResetAction = false;
        public float time
        {
            get => Base.time;
            set
            {
                AddResetAction(ref _hasTimeResetAction, () => Base.time = 0);
                Base.time = value;
            }
        }

        private bool _hasTimeSamplesResetAction = false;
        public int timeSamples
        {
            get => Base.timeSamples;
            set
            {
                AddResetAction(ref _hasTimeSamplesResetAction, () => Base.timeSamples = 0);
                Base.timeSamples = value;
            }
        }

        private bool _hasClipResetAction = false;
        public AudioClip clip
        {
            get => Base.clip;
            set
            {
                AddResetAction(ref _hasClipResetAction, () => Base.clip = default);
                Base.clip = value;
            }
        }

        private bool _hasOutputAudioMixerGroupResetAction = false;
        public AudioMixerGroup outputAudioMixerGroup
        {
            get => Base.outputAudioMixerGroup;
            set
            {
                AddResetAction(ref _hasOutputAudioMixerGroupResetAction, () => Base.outputAudioMixerGroup = default);
                Base.outputAudioMixerGroup = value;
            }
        }

#if (UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5
        private bool _hasGamepadSpeakerOutputTypeResetAction = false;
        public GamepadSpeakerOutputType gamepadSpeakerOutputType
        {
            get => Base.gamepadSpeakerOutputType;
            set
            {
                AddResetAction(ref _hasGamepadSpeakerOutputTypeResetAction, () => Base.gamepadSpeakerOutputType = UnityEngine.GamepadSpeakerOutputType.Speaker);
                Base.gamepadSpeakerOutputType = value;
            }
        }
#endif

        private bool _hasLoopResetAction = false;
        public bool loop
        {
            get => Base.loop;
            set
            {
                AddResetAction(ref _hasLoopResetAction, () => Base.loop = false);
                Base.loop = value;
            }
        }

        private bool _hasIgnoreListenerVolumeResetAction = false;
        public bool ignoreListenerVolume
        {
            get => Base.ignoreListenerVolume;
            set
            {
                AddResetAction(ref _hasIgnoreListenerVolumeResetAction, () => Base.ignoreListenerVolume = false);
                Base.ignoreListenerVolume = value;
            }
        }

        private bool _hasPlayOnAwakeResetAction = false;
        public bool playOnAwake
        {
            get => Base.playOnAwake;
            set
            {
                AddResetAction(ref _hasPlayOnAwakeResetAction, () => Base.playOnAwake = true);
                Base.playOnAwake = value;
            }
        }

        private bool _hasIgnoreListenerPauseResetAction = false;
        public bool ignoreListenerPause
        {
            get => Base.ignoreListenerPause;
            set
            {
                AddResetAction(ref _hasIgnoreListenerPauseResetAction, () => Base.ignoreListenerPause = false);
                Base.ignoreListenerPause = value;
            }
        }

        private bool _hasVelocityUpdateModeResetAction = false;
        public AudioVelocityUpdateMode velocityUpdateMode
        {
            get => Base.velocityUpdateMode;
            set
            {
                AddResetAction(ref _hasVelocityUpdateModeResetAction, () => Base.velocityUpdateMode = UnityEngine.AudioVelocityUpdateMode.Auto);
                Base.velocityUpdateMode = value;
            }
        }

        private bool _hasPanStereoResetAction = false;
        public float panStereo
        {
            get => Base.panStereo;
            set
            {
                AddResetAction(ref _hasPanStereoResetAction, () => Base.panStereo = 0);
                Base.panStereo = value;
            }
        }

        private bool _hasSpatialBlendResetAction = false;
        public float spatialBlend
        {
            get => Base.spatialBlend;
            set
            {
                AddResetAction(ref _hasSpatialBlendResetAction, () => Base.spatialBlend = 0);
                Base.spatialBlend = value;
            }
        }

        private bool _hasSpatializeResetAction = false;
        public bool spatialize
        {
            get => Base.spatialize;
            set
            {
                AddResetAction(ref _hasSpatializeResetAction, () => Base.spatialize = false);
                Base.spatialize = value;
            }
        }

        private bool _hasSpatializePostEffectsResetAction = false;
        public bool spatializePostEffects
        {
            get => Base.spatializePostEffects;
            set
            {
                AddResetAction(ref _hasSpatializePostEffectsResetAction, () => Base.spatializePostEffects = false);
                Base.spatializePostEffects = value;
            }
        }

        private bool _hasReverbZoneMixResetAction = false;
        public float reverbZoneMix
        {
            get => Base.reverbZoneMix;
            set
            {
                AddResetAction(ref _hasReverbZoneMixResetAction, () => Base.reverbZoneMix = 1);
                Base.reverbZoneMix = value;
            }
        }

        private bool _hasBypassEffectsResetAction = false;
        public bool bypassEffects
        {
            get => Base.bypassEffects;
            set
            {
                AddResetAction(ref _hasBypassEffectsResetAction, () => Base.bypassEffects = false);
                Base.bypassEffects = value;
            }
        }

        private bool _hasBypassListenerEffectsResetAction = false;
        public bool bypassListenerEffects
        {
            get => Base.bypassListenerEffects;
            set
            {
                AddResetAction(ref _hasBypassListenerEffectsResetAction, () => Base.bypassListenerEffects = false);
                Base.bypassListenerEffects = value;
            }
        }

        private bool _hasBypassReverbZonesResetAction = false;
        public bool bypassReverbZones
        {
            get => Base.bypassReverbZones;
            set
            {
                AddResetAction(ref _hasBypassReverbZonesResetAction, () => Base.bypassReverbZones = false);
                Base.bypassReverbZones = value;
            }
        }

        private bool _hasDopplerLevelResetAction = false;
        public float dopplerLevel
        {
            get => Base.dopplerLevel;
            set
            {
                AddResetAction(ref _hasDopplerLevelResetAction, () => Base.dopplerLevel = 1);
                Base.dopplerLevel = value;
            }
        }

        private bool _hasSpreadResetAction = false;
        public float spread
        {
            get => Base.spread;
            set
            {
                AddResetAction(ref _hasSpreadResetAction, () => Base.spread = 0);
                Base.spread = value;
            }
        }

        private bool _hasPriorityResetAction = false;
        public int priority
        {
            get => Base.priority;
            set
            {
                AddResetAction(ref _hasPriorityResetAction, () => Base.priority = 128);
                Base.priority = value;
            }
        }

        private bool _hasMuteResetAction = false;
        public bool mute
        {
            get => Base.mute;
            set
            {
                AddResetAction(ref _hasMuteResetAction, () => Base.mute = false);
                Base.mute = value;
            }
        }

        private bool _hasMinDistanceResetAction = false;
        public float minDistance
        {
            get => Base.minDistance;
            set
            {
                AddResetAction(ref _hasMinDistanceResetAction, () => Base.minDistance = 1);
                Base.minDistance = value;
            }
        }

        private bool _hasMaxDistanceResetAction = false;
        public float maxDistance
        {
            get => Base.maxDistance;
            set
            {
                AddResetAction(ref _hasMaxDistanceResetAction, () => Base.maxDistance = 500);
                Base.maxDistance = value;
            }
        }

        private bool _hasRolloffModeResetAction = false;
        public AudioRolloffMode rolloffMode
        {
            get => Base.rolloffMode;
            set
            {
                AddResetAction(ref _hasRolloffModeResetAction, () => Base.rolloffMode = UnityEngine.AudioRolloffMode.Logarithmic);
                Base.rolloffMode = value;
            }
        }

    }
}
