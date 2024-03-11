using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class EffectField : InteractiveComponent
	{
#pragma warning disable 414
        [SerializeField] AudioID _enterExitSound = default;
		[SerializeField, Volume(true)] float _targetVolume = 0.5f;
		[SerializeField, Frequency] float _lowPassFreq = 800f;
#pragma warning restore 414
        public override void OnInZoneChanged(bool isInZone)
		{
			BroAudio.Play(_enterExitSound);

#if !UNITY_WEBGL
			if (isInZone)
			{
				BroAudio.SetVolume(BroAudioType.All, _targetVolume);
				BroAudio.SetEffect(Effect.LowPass(_lowPassFreq, 0.5f));
			}
			else
			{
				BroAudio.SetVolume(BroAudioType.All, BroAdvice.FullVolume, 1f);
				BroAudio.SetEffect(Effect.LowPass(Effect.Defaults.LowPass, 1f));
			} 
#endif
		}
	}
}