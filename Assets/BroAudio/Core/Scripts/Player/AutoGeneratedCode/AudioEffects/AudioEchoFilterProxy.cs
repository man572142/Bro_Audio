// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioEchoFilterProxy : IAudioEffectModifier, IAudioEchoFilterProxy
    {
        private AudioEchoFilter _source;
        public AudioEchoFilterProxy(AudioEchoFilter source) => _source = source;

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

        private bool _isDecayRatioModified = false;
        public float decayRatio
        {
            get => _source.decayRatio;
            set
            {
                _isDecayRatioModified = true;
                _source.decayRatio = value;
            }
        }

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

        private bool _isWetMixModified = false;
        public float wetMix
        {
            get => _source.wetMix;
            set
            {
                _isWetMixModified = true;
                _source.wetMix = value;
            }
        }

        public void TransferValueTo<T>(T target) where T : UnityEngine.Behaviour
        {
            if (_source == null || !(target is AudioEchoFilter targetComponent)) return;

            if (_isDelayModified) targetComponent.delay = _source.delay;
            if (_isDecayRatioModified) targetComponent.decayRatio = _source.decayRatio;
            if (_isDryMixModified) targetComponent.dryMix = _source.dryMix;
            if (_isWetMixModified) targetComponent.wetMix = _source.wetMix;

            _source = targetComponent;
        }
    }
}
