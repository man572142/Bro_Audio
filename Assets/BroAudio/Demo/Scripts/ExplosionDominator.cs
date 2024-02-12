using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class ExplosionDominator : InteractiveComponent
	{
		[SerializeField] ParticleSystem _particle = null;
		[SerializeField] ParticleSystem _fog = null;
		[SerializeField] float _playInterval = default;
		[SerializeField] AudioID _explosion = default;
		[SerializeField] AudioID _warningVoice = default;

		[SerializeField, Frequency] float _lowPassFrequency = default;

		private Coroutine _coroutine;

		public override void OnInZoneChanged(bool isInZone)
		{
			if(_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}

			if(isInZone)
			{
				_coroutine = StartCoroutine(KeepLooping());
			}
			else
			{
				_particle.Stop();
				_fog.gameObject.SetActive(true);
				_fog.Play();
			}
		}

		private IEnumerator KeepLooping()
		{
			while(true)
			{
				if(_particle.isPlaying)
				{
					yield return new WaitWhile(() => _particle.isPlaying);
				}

				_particle.Play();
				BroAudio.Play(_explosion).AsDominator().LowPassOthers(_lowPassFrequency);
				BroAudio.Play(_warningVoice).AsDominator().LowPassOthers(_lowPassFrequency);
				_fog.Stop();
				_fog.gameObject.SetActive(false);
				yield return new WaitForSeconds(_playInterval);
			}
		}
	}
}