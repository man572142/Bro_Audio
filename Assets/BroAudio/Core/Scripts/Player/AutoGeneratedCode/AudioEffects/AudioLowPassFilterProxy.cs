// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioLowPassFilterProxy : IAudioEffectModifier, IAudioLowPassFilterProxy
    {
        private AudioLowPassFilter _source;
        public AudioLowPassFilterProxy(AudioLowPassFilter source) => _source = source;

        private bool _isCustomCutoffCurveModified = false;
        public AnimationCurve customCutoffCurve
        {
            get => _source.customCutoffCurve;
            set
            {
                _isCustomCutoffCurveModified = true;
                _source.customCutoffCurve = value;
            }
        }

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

        private bool _isLowpassResonanceQModified = false;
        public float lowpassResonanceQ
        {
            get => _source.lowpassResonanceQ;
            set
            {
                _isLowpassResonanceQModified = true;
                _source.lowpassResonanceQ = value;
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
            if (_source == null || !(target is AudioLowPassFilter targetComponent)) return;

            if (_isCustomCutoffCurveModified) targetComponent.customCutoffCurve = _source.customCutoffCurve;
            if (_isCutoffFrequencyModified) targetComponent.cutoffFrequency = _source.cutoffFrequency;
            if (_isLowpassResonanceQModified) targetComponent.lowpassResonanceQ = _source.lowpassResonanceQ;
            if (_isEnabledModified) targetComponent.enabled = _source.enabled;

            _source = targetComponent;
        }
    }
}
