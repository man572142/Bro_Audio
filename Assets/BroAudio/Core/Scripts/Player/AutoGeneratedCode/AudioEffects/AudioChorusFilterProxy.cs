// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioChorusFilterProxy : IAudioEffectModifier, IAudioChorusFilterProxy
    {
        private AudioChorusFilter _source;
        public AudioChorusFilterProxy(AudioChorusFilter source) => _source = source;

        private bool _isDryMixModified = false;
        public float dryMix
        {
            get => _source.dryMix;
            set
            {
                _isDryMixModified = true;
                _source.dryMix = value;
            }
        }

        private bool _isWetMix1Modified = false;
        public float wetMix1
        {
            get => _source.wetMix1;
            set
            {
                _isWetMix1Modified = true;
                _source.wetMix1 = value;
            }
        }

        private bool _isWetMix2Modified = false;
        public float wetMix2
        {
            get => _source.wetMix2;
            set
            {
                _isWetMix2Modified = true;
                _source.wetMix2 = value;
            }
        }

        private bool _isWetMix3Modified = false;
        public float wetMix3
        {
            get => _source.wetMix3;
            set
            {
                _isWetMix3Modified = true;
                _source.wetMix3 = value;
            }
        }

        private bool _isDelayModified = false;
        public float delay
        {
            get => _source.delay;
            set
            {
                _isDelayModified = true;
                _source.delay = value;
            }
        }

        private bool _isRateModified = false;
        public float rate
        {
            get => _source.rate;
            set
            {
                _isRateModified = true;
                _source.rate = value;
            }
        }

        private bool _isDepthModified = false;
        public float depth
        {
            get => _source.depth;
            set
            {
                _isDepthModified = true;
                _source.depth = value;
            }
        }

        private bool _isEnabledModified = false;
        public bool enabled
        {
            get => _source.enabled;
            set
            {
                _isEnabledModified = true;
                _source.enabled = value;
            }
        }

        public void TransferValueTo<T>(T target) where T : UnityEngine.Behaviour
        {
            if (_source == null || !(target is AudioChorusFilter targetComponent)) return;

            if (_isDryMixModified) targetComponent.dryMix = _source.dryMix;
            if (_isWetMix1Modified) targetComponent.wetMix1 = _source.wetMix1;
            if (_isWetMix2Modified) targetComponent.wetMix2 = _source.wetMix2;
            if (_isWetMix3Modified) targetComponent.wetMix3 = _source.wetMix3;
            if (_isDelayModified) targetComponent.delay = _source.delay;
            if (_isRateModified) targetComponent.rate = _source.rate;
            if (_isDepthModified) targetComponent.depth = _source.depth;
            if (_isEnabledModified) targetComponent.enabled = _source.enabled;

            _source = targetComponent;
        }
    }
}
