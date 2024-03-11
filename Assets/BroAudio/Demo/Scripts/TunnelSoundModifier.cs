using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class TunnelSoundModifier : InteractiveComponent
    {
        [SerializeField] AudioID _targetAmbience = default;
        [SerializeField, Volume] float _absorbedvolume = 0.3f;
        [SerializeField, Volume(true)] float _reflectionBoostVolume = 1.3f;
        [SerializeField] BroAudioType _reflectionBoostType = default;
        [SerializeField] float _transitionTime = 2f;

        public override void OnInZoneChanged(bool isInZone)
        {
            if(isInZone)
            {
                BroAudio.SetVolume(_targetAmbience, _absorbedvolume, _transitionTime);
                BroAudio.SetVolume(_reflectionBoostType, _reflectionBoostVolume, _transitionTime);
            }
            else
            {
                BroAudio.SetVolume(_targetAmbience, Extension.AudioConstant.FullVolume, _transitionTime);
				BroAudio.SetVolume(_reflectionBoostType, 1f, _transitionTime);
			}
        }
    } 
}
