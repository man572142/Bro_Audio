using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
	public class EscapingZone : InteractiveComponent
	{
		[SerializeField] AudioID _nightTimeBGM = default;
		[SerializeField] AudioID _dayTimeBGM = default;
		[SerializeField] float _transitionTime = default;

		[Header("API Text")]
		[SerializeField] Animator _apiAnimator = null;
		[SerializeField] string _apiTriggerName = null;
		[SerializeField] APIText _apiSetter  = null;

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
				_apiSetter.SetAPI();
				_apiAnimator.SetTrigger(_apiTriggerName);
				_lightAnimator.SetBool(_ligghtAnimParameterName, _isNightTime);

				AudioID id = _isNightTime ? _nightTimeBGM : _dayTimeBGM;
				BroAudio.Play(id).AsBGM().SetTransition(Transition.CrossFade, _transitionTime);
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