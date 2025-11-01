// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;
using System;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : IDisposable, IAudioSourceProxy
    {
        private AudioSource _source;
        public AudioSourceProxy(AudioSource source) => _source = source;

        private bool _isVolumeModified = false;
        public float volume
        {
            get => _source.volume;
            set
            {
                _isVolumeModified = true;
                _source.volume = value;
            }
        }

        private bool _isPitchModified = false;
        public float pitch
        {
            get => _source.pitch;
            set
            {
                _isPitchModified = true;
                _source.pitch = value;
            }
        }

        private bool _isTimeModified = false;
        public float time
        {
            get => _source.time;
            set
            {
                _isTimeModified = true;
                _source.time = value;
            }
        }

        private bool _isTimeSamplesModified = false;
        public int timeSamples
        {
            get => _source.timeSamples;
            set
            {
                _isTimeSamplesModified = true;
                _source.timeSamples = value;
            }
        }

        private bool _isOutputAudioMixerGroupModified = false;
        public AudioMixerGroup outputAudioMixerGroup
        {
            get => _source.outputAudioMixerGroup;
            set
            {
                _isOutputAudioMixerGroupModified = true;
                _source.outputAudioMixerGroup = value;
            }
        }

#if (UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5
        private bool _isGamepadSpeakerOutputTypeModified = false;
        public GamepadSpeakerOutputType gamepadSpeakerOutputType
        {
            get => _source.gamepadSpeakerOutputType;
            set
            {
                _isGamepadSpeakerOutputTypeModified = true;
                _source.gamepadSpeakerOutputType = value;
            }
        }
#endif

        private bool _isLoopModified = false;
        public bool loop
        {
            get => _source.loop;
            set
            {
                _isLoopModified = true;
                _source.loop = value;
            }
        }

        private bool _isIgnoreListenerVolumeModified = false;
        public bool ignoreListenerVolume
        {
            get => _source.ignoreListenerVolume;
            set
            {
                _isIgnoreListenerVolumeModified = true;
                _source.ignoreListenerVolume = value;
            }
        }

        private bool _isPlayOnAwakeModified = false;
        public bool playOnAwake
        {
            get => _source.playOnAwake;
            set
            {
                _isPlayOnAwakeModified = true;
                _source.playOnAwake = value;
            }
        }

        private bool _isIgnoreListenerPauseModified = false;
        public bool ignoreListenerPause
        {
            get => _source.ignoreListenerPause;
            set
            {
                _isIgnoreListenerPauseModified = true;
                _source.ignoreListenerPause = value;
            }
        }

        private bool _isVelocityUpdateModeModified = false;
        public AudioVelocityUpdateMode velocityUpdateMode
        {
            get => _source.velocityUpdateMode;
            set
            {
                _isVelocityUpdateModeModified = true;
                _source.velocityUpdateMode = value;
            }
        }

        private bool _isPanStereoModified = false;
        public float panStereo
        {
            get => _source.panStereo;
            set
            {
                _isPanStereoModified = true;
                _source.panStereo = value;
            }
        }

        private bool _isSpatialBlendModified = false;
        public float spatialBlend
        {
            get => _source.spatialBlend;
            set
            {
                _isSpatialBlendModified = true;
                _source.spatialBlend = value;
            }
        }

        private bool _isSpatializeModified = false;
        public bool spatialize
        {
            get => _source.spatialize;
            set
            {
                _isSpatializeModified = true;
                _source.spatialize = value;
            }
        }

        private bool _isSpatializePostEffectsModified = false;
        public bool spatializePostEffects
        {
            get => _source.spatializePostEffects;
            set
            {
                _isSpatializePostEffectsModified = true;
                _source.spatializePostEffects = value;
            }
        }

        private bool _isReverbZoneMixModified = false;
        public float reverbZoneMix
        {
            get => _source.reverbZoneMix;
            set
            {
                _isReverbZoneMixModified = true;
                _source.reverbZoneMix = value;
            }
        }

        private bool _isBypassEffectsModified = false;
        public bool bypassEffects
        {
            get => _source.bypassEffects;
            set
            {
                _isBypassEffectsModified = true;
                _source.bypassEffects = value;
            }
        }

        private bool _isBypassListenerEffectsModified = false;
        public bool bypassListenerEffects
        {
            get => _source.bypassListenerEffects;
            set
            {
                _isBypassListenerEffectsModified = true;
                _source.bypassListenerEffects = value;
            }
        }

        private bool _isBypassReverbZonesModified = false;
        public bool bypassReverbZones
        {
            get => _source.bypassReverbZones;
            set
            {
                _isBypassReverbZonesModified = true;
                _source.bypassReverbZones = value;
            }
        }

        private bool _isDopplerLevelModified = false;
        public float dopplerLevel
        {
            get => _source.dopplerLevel;
            set
            {
                _isDopplerLevelModified = true;
                _source.dopplerLevel = value;
            }
        }

        private bool _isSpreadModified = false;
        public float spread
        {
            get => _source.spread;
            set
            {
                _isSpreadModified = true;
                _source.spread = value;
            }
        }

        private bool _isPriorityModified = false;
        public int priority
        {
            get => _source.priority;
            set
            {
                _isPriorityModified = true;
                _source.priority = value;
            }
        }

        private bool _isMuteModified = false;
        public bool mute
        {
            get => _source.mute;
            set
            {
                _isMuteModified = true;
                _source.mute = value;
            }
        }

        private bool _isMinDistanceModified = false;
        public float minDistance
        {
            get => _source.minDistance;
            set
            {
                _isMinDistanceModified = true;
                _source.minDistance = value;
            }
        }

        private bool _isMaxDistanceModified = false;
        public float maxDistance
        {
            get => _source.maxDistance;
            set
            {
                _isMaxDistanceModified = true;
                _source.maxDistance = value;
            }
        }

        private bool _isRolloffModeModified = false;
        public AudioRolloffMode rolloffMode
        {
            get => _source.rolloffMode;
            set
            {
                _isRolloffModeModified = true;
                _source.rolloffMode = value;
            }
        }

        private bool _isClipModified = false;
        public AudioClip clip
        {
            get => _source.clip;
            set
            {
                _isClipModified = true;
                _source.clip = value;
            }
        }

        public void Dispose()
        {
            if (_isVolumeModified) {_source.volume = 1f; _isVolumeModified = false;}
            if (_isPitchModified) {_source.pitch = 1f; _isPitchModified = false;}
            if (_isTimeModified) {_source.time = 0f; _isTimeModified = false;}
            if (_isTimeSamplesModified) {_source.timeSamples = 0; _isTimeSamplesModified = false;}
            if (_isOutputAudioMixerGroupModified) {_source.outputAudioMixerGroup = null; _isOutputAudioMixerGroupModified = false;}
#if (UNITY_EDITOR && UNITY_2021_3_OR_NEWER) || UNITY_PS4 || UNITY_PS5
            if (_isGamepadSpeakerOutputTypeModified) {_source.gamepadSpeakerOutputType = UnityEngine.GamepadSpeakerOutputType.Speaker; _isGamepadSpeakerOutputTypeModified = false;}
#endif
            if (_isLoopModified) {_source.loop = false; _isLoopModified = false;}
            if (_isIgnoreListenerVolumeModified) {_source.ignoreListenerVolume = false; _isIgnoreListenerVolumeModified = false;}
            if (_isPlayOnAwakeModified) {_source.playOnAwake = true; _isPlayOnAwakeModified = false;}
            if (_isIgnoreListenerPauseModified) {_source.ignoreListenerPause = false; _isIgnoreListenerPauseModified = false;}
            if (_isVelocityUpdateModeModified) {_source.velocityUpdateMode = UnityEngine.AudioVelocityUpdateMode.Auto; _isVelocityUpdateModeModified = false;}
            if (_isPanStereoModified) {_source.panStereo = 0f; _isPanStereoModified = false;}
            if (_isSpatialBlendModified) {_source.spatialBlend = 0f; _isSpatialBlendModified = false;}
            if (_isSpatializeModified) {_source.spatialize = false; _isSpatializeModified = false;}
            if (_isSpatializePostEffectsModified) {_source.spatializePostEffects = false; _isSpatializePostEffectsModified = false;}
            if (_isReverbZoneMixModified) {_source.reverbZoneMix = 1f; _isReverbZoneMixModified = false;}
            if (_isBypassEffectsModified) {_source.bypassEffects = false; _isBypassEffectsModified = false;}
            if (_isBypassListenerEffectsModified) {_source.bypassListenerEffects = false; _isBypassListenerEffectsModified = false;}
            if (_isBypassReverbZonesModified) {_source.bypassReverbZones = false; _isBypassReverbZonesModified = false;}
            if (_isDopplerLevelModified) {_source.dopplerLevel = 1f; _isDopplerLevelModified = false;}
            if (_isSpreadModified) {_source.spread = 0f; _isSpreadModified = false;}
            if (_isPriorityModified) {_source.priority = 128; _isPriorityModified = false;}
            if (_isMuteModified) {_source.mute = false; _isMuteModified = false;}
            if (_isMinDistanceModified) {_source.minDistance = 1f; _isMinDistanceModified = false;}
            if (_isMaxDistanceModified) {_source.maxDistance = 500f; _isMaxDistanceModified = false;}
            if (_isRolloffModeModified) {_source.rolloffMode = UnityEngine.AudioRolloffMode.Logarithmic; _isRolloffModeModified = false;}
            if (_isClipModified) {_source.clip = null; _isClipModified = false;}
        }
    }
}
