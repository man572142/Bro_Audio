// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioReverbFilterProxy : IAudioEffectModifier, IAudioReverbFilterProxy
    {
        private AudioReverbFilter _source;
        public AudioReverbFilterProxy(AudioReverbFilter source) => _source = source;

        private bool _isReverbPresetModified = false;
        public AudioReverbPreset reverbPreset
        {
            get => _source.reverbPreset;
            set
            {
                _isReverbPresetModified = true;
                _source.reverbPreset = value;
            }
        }

        private bool _isDryLevelModified = false;
        public float dryLevel
        {
            get => _source.dryLevel;
            set
            {
                _isDryLevelModified = true;
                _source.dryLevel = value;
            }
        }

        private bool _isRoomModified = false;
        public float room
        {
            get => _source.room;
            set
            {
                _isRoomModified = true;
                _source.room = value;
            }
        }

        private bool _isRoomHFModified = false;
        public float roomHF
        {
            get => _source.roomHF;
            set
            {
                _isRoomHFModified = true;
                _source.roomHF = value;
            }
        }

        private bool _isDecayTimeModified = false;
        public float decayTime
        {
            get => _source.decayTime;
            set
            {
                _isDecayTimeModified = true;
                _source.decayTime = value;
            }
        }

        private bool _isDecayHFRatioModified = false;
        public float decayHFRatio
        {
            get => _source.decayHFRatio;
            set
            {
                _isDecayHFRatioModified = true;
                _source.decayHFRatio = value;
            }
        }

        private bool _isReflectionsLevelModified = false;
        public float reflectionsLevel
        {
            get => _source.reflectionsLevel;
            set
            {
                _isReflectionsLevelModified = true;
                _source.reflectionsLevel = value;
            }
        }

        private bool _isReflectionsDelayModified = false;
        public float reflectionsDelay
        {
            get => _source.reflectionsDelay;
            set
            {
                _isReflectionsDelayModified = true;
                _source.reflectionsDelay = value;
            }
        }

        private bool _isReverbLevelModified = false;
        public float reverbLevel
        {
            get => _source.reverbLevel;
            set
            {
                _isReverbLevelModified = true;
                _source.reverbLevel = value;
            }
        }

        private bool _isReverbDelayModified = false;
        public float reverbDelay
        {
            get => _source.reverbDelay;
            set
            {
                _isReverbDelayModified = true;
                _source.reverbDelay = value;
            }
        }

        private bool _isDiffusionModified = false;
        public float diffusion
        {
            get => _source.diffusion;
            set
            {
                _isDiffusionModified = true;
                _source.diffusion = value;
            }
        }

        private bool _isDensityModified = false;
        public float density
        {
            get => _source.density;
            set
            {
                _isDensityModified = true;
                _source.density = value;
            }
        }

        private bool _isHfReferenceModified = false;
        public float hfReference
        {
            get => _source.hfReference;
            set
            {
                _isHfReferenceModified = true;
                _source.hfReference = value;
            }
        }

        private bool _isRoomLFModified = false;
        public float roomLF
        {
            get => _source.roomLF;
            set
            {
                _isRoomLFModified = true;
                _source.roomLF = value;
            }
        }

        private bool _isLfReferenceModified = false;
        public float lfReference
        {
            get => _source.lfReference;
            set
            {
                _isLfReferenceModified = true;
                _source.lfReference = value;
            }
        }

        public void TransferValueTo<T>(T target) where T : UnityEngine.Behaviour
        {
            if (_source == null || !(target is AudioReverbFilter targetComponent)) return;

            if (_isReverbPresetModified) targetComponent.reverbPreset = _source.reverbPreset;
            if (_isDryLevelModified) targetComponent.dryLevel = _source.dryLevel;
            if (_isRoomModified) targetComponent.room = _source.room;
            if (_isRoomHFModified) targetComponent.roomHF = _source.roomHF;
            if (_isDecayTimeModified) targetComponent.decayTime = _source.decayTime;
            if (_isDecayHFRatioModified) targetComponent.decayHFRatio = _source.decayHFRatio;
            if (_isReflectionsLevelModified) targetComponent.reflectionsLevel = _source.reflectionsLevel;
            if (_isReflectionsDelayModified) targetComponent.reflectionsDelay = _source.reflectionsDelay;
            if (_isReverbLevelModified) targetComponent.reverbLevel = _source.reverbLevel;
            if (_isReverbDelayModified) targetComponent.reverbDelay = _source.reverbDelay;
            if (_isDiffusionModified) targetComponent.diffusion = _source.diffusion;
            if (_isDensityModified) targetComponent.density = _source.density;
            if (_isHfReferenceModified) targetComponent.hfReference = _source.hfReference;
            if (_isRoomLFModified) targetComponent.roomLF = _source.roomLF;
            if (_isLfReferenceModified) targetComponent.lfReference = _source.lfReference;

            _source = targetComponent;
        }
    }
}
