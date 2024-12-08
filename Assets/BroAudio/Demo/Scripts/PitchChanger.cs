using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class PitchChanger : MonoBehaviour
	{
		[SerializeField, Pitch] float _pitch = 1f;
		[SerializeField] BroAudioType _targetAudioType = BroAudioType.All;

        private void OnTriggerEnter(Collider other)
        {
            if(other.gameObject.CompareTag("Player"))
            {
                BroAudio.SetPitch(_targetAudioType, _pitch);
            }
        }
    }
}
