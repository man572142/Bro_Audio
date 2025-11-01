using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class EffectField : InteractiveComponent
	{
#pragma warning disable 414
        [SerializeField] SoundID _enterExitSound = default;
		[SerializeField, Frequency] float _lowPassFreq = 800f;
		[SerializeField] float _fadeTime = 1f;
#pragma warning restore 414
        public override void OnInZoneChanged(bool isInZone)
		{
			BroAudio.Play(_enterExitSound);

#if !UNITY_WEBGL
			if (isInZone)
			{
				BroAudio.SetEffect(Effect.LowPass(_lowPassFreq, _fadeTime));
			}
			else
			{
                BroAudio.SetEffect(Effect.ResetLowPass(_fadeTime));
			} 
#endif
		}
	}
}