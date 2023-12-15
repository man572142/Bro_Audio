using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class EffectField : InteractiveComponent
	{
		[SerializeField] AudioID _enterExitSound = default;
		[SerializeField, Range(0f,1f)] float _targetVolume = 0.5f;
		[SerializeField, Range(10f, 22000f)] float _targetFreq = 800f;

		protected override bool ListenToInteractiveZone() => true;

		public override void OnInZoneChanged(bool isInZone)
		{
			BroAudio.Play(_enterExitSound);

			if(isInZone)
			{
				BroAudio.SetEffect(Effect.Volume(_targetVolume, 1f));
				BroAudio.SetEffect(Effect.HighCut(_targetFreq, 0.5f));
			}
			else
			{
				BroAudio.SetEffect(Effect.Volume(Effect.Defaults.Volume, 1f));
				BroAudio.SetEffect(Effect.HighCut(Effect.Defaults.HighCut, 1f));
			}
		}
	}
}