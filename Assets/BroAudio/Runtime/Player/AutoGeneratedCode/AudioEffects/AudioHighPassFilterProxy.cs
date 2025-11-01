// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioHighPassFilterProxy : IAudioEffectModifier, IAudioHighPassFilterProxy
    {
        private AudioHighPassFilter _source;
        public AudioHighPassFilterProxy(AudioHighPassFilter source) => _source = source;

        private bool _isCutoffFrequencyModified = false;
        public float cutoffFrequency
        {
            get => _source.cutoffFrequency;
            set
            {
                _isCutoffFrequencyModified = true;
                _source.cutoffFrequency = value;
            }
        }

        private bool _isHighpassResonanceQModified = false;
        public float highpassResonanceQ
        {
            get => _source.highpassResonanceQ;
            set
            {
                _isHighpassResonanceQModified = true;
                _source.highpassResonanceQ = value;
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
            if (_source == null || !(target is AudioHighPassFilter targetComponent)) return;

            if (_isCutoffFrequencyModified) targetComponent.cutoffFrequency = _source.cutoffFrequency;
            if (_isHighpassResonanceQModified) targetComponent.highpassResonanceQ = _source.highpassResonanceQ;
            if (_isEnabledModified) targetComponent.enabled = _source.enabled;

            _source = targetComponent;
        }
    }
}
