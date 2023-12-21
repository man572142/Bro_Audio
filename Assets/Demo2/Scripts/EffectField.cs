using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class EffectField : InteractiveComponent
	{
		[SerializeField] AudioID _enterExitSound = default;
		[SerializeField, Volume(true)] float _targetVolume = 0.5f;
		[SerializeField, Frequency] float _lowPassFreq = 800f;

		public override void OnInZoneChanged(bool isInZone)
		{
			BroAudio.Play(_enterExitSound);

			if(isInZone)
			{
				BroAudio.SetEffect(Effect.Volume(_targetVolume, 1f));
				BroAudio.SetEffect(Effect.LowPass(_lowPassFreq, 0.5f));
			}
			else
			{
				BroAudio.SetEffect(Effect.Volume(Effect.Defaults.Volume, 1f));
				BroAudio.SetEffect(Effect.LowPass(Effect.Defaults.LowPass, 1f));
			}
		}
	}
}