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

		[SerializeField, Frequency] float _lowPassFrequency = default;

		private Coroutine _coroutine;
		private IAudioPlayer _explosionPlayer = null;

        private void PlayAudio()
        {
            _explosionPlayer = BroAudio.Play(_explosion);
            _explosionPlayer.AsDominator().LowPassOthers(_lowPassFrequency);
        }

        public override void OnInZoneChanged(bool isInZone)
		{
			if(_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}

			if(isInZone)
			{
				_coroutine = StartCoroutine(KeepPlaying());
			}
			else
			{
				_particle.Stop();
				_fog.gameObject.SetActive(true);
				_fog.Play();
			}
		}

		private IEnumerator KeepPlaying()
		{
			while(true)
            {
                if (_particle.isPlaying)
                {
                    yield return new WaitWhile(() => _particle.isPlaying);
                }

                _particle.Play();
                _fog.Stop();
                _fog.gameObject.SetActive(false);

                PlayAudio();
                
                yield return new WaitForSeconds(_playInterval);
            }
        }
    }
}