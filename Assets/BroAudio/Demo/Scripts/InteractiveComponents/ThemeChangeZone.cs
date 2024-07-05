using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class ThemeChangeZone : InteractiveComponent
	{
		[SerializeField] SoundID _bgm = default;
		[SerializeField] Transition _transition = default;
		[SerializeField] float _transitionTime = default;

		[Header("Change Mood")]
		[SerializeField] Animator _lightAnimator = null;
		[SerializeField] string _ligghtAnimParameterName = null;

		private bool _isNightTime = false;
		private bool _canChange = true;

		public override void OnInZoneChanged(bool isInZone)
		{
			if(isInZone && _canChange)
			{
				_isNightTime = !_isNightTime;
				_lightAnimator.SetBool(_ligghtAnimParameterName, _isNightTime);

				// The BGM is set to PlaybackMode.Sequence
				BroAudio.Play(_bgm).AsBGM().SetTransition(_transition, _transitionTime);
				StartCoroutine(PreventChangePeriod());
			}	
		}

		private IEnumerator PreventChangePeriod()
		{
			_canChange = false;
			yield return new WaitForSeconds(_transitionTime);
			_canChange = true;
		}
	}
}