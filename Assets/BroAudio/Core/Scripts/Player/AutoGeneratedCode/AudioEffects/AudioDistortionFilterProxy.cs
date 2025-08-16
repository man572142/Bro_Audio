// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public class AudioDistortionFilterProxy : IAudioEffectModifier, IAudioDistortionFilterProxy
    {
        private AudioDistortionFilter _source;
        public AudioDistortionFilterProxy(AudioDistortionFilter source) => _source = source;

        private bool _isDistortionLevelModified = false;
        public float distortionLevel
        {
            get => _source.distortionLevel;
            set
            {
                _isDistortionLevelModified = true;
                _source.distortionLevel = value;
            }
        }

        public void TransferValueTo<T>(T target) where T : UnityEngine.Behaviour
        {
            if (_source == null || !(target is AudioDistortionFilter targetComponent)) return;

            if (_isDistortionLevelModified) targetComponent.distortionLevel = _source.distortionLevel;

            _source = targetComponent;
        }
    }
}
