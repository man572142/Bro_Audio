using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class TunnelSoundModifier : InteractiveComponent
    {
        [SerializeField] BroAudioType _targetType = default;
        [SerializeField, Volume] float _absorbedvolume = 0.3f;
        [SerializeField] float _transitionTime = 2f;

        private bool _hasStarted = false;

        public override void OnInZoneChanged(bool isInZone)
        {
            if(isInZone)
            {
                BroAudio.SetVolume(_targetType, _absorbedvolume, _hasStarted ? _transitionTime : 0f);
                _hasStarted = true;
            }
            else
            {
                BroAudio.SetVolume(_targetType, Extension.AudioConstant.FullVolume, _transitionTime);
			}
        }
    } 
}
