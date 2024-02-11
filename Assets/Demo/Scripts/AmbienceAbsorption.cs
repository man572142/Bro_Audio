using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class AmbienceAbsorption : InteractiveComponent
    {
        [SerializeField] AudioID _targetAmbience = default;
        [SerializeField, Volume] float _absorbedvolume = 0.2f;

        private bool _hasStartSet = false;

        public override void OnInZoneChanged(bool isInZone)
        {
            if(isInZone)
            {
                BroAudio.SetVolume(_targetAmbience, _absorbedvolume, _hasStartSet ? 1f : 0f);
                _hasStartSet= true;
            }
            else
            {
                BroAudio.SetVolume(_targetAmbience, Extension.AudioConstant.FullVolume, 2f);
            }
        }
    } 
}
