using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
	public class EscapingZone : InteractiveComponent
	{
		[SerializeField] AudioID _escapingBGM = default;
		[SerializeField] float _transitionTime = default;

		[Header("API Text")]
		[SerializeField] Animator _animator = null;
		[SerializeField] string _triggerName = null;
		[SerializeField] APIText _apiSetter  = null;

		[Header("Change Light")]
		[SerializeField] Light _directionalLight = null;
		[SerializeField] GameObject _moodLight = null;
		[SerializeField] Vector3 _targetRotation = default;
		[SerializeField] Color _targetColor = default;
		
		private bool _hasChanged = false;

		protected override bool ListenToInteractiveZone() => true;

		public override void OnInZoneChanged(bool isInZone)
		{
			if(isInZone && !_hasChanged)
			{
				_apiSetter.SetAPI();
				_animator.SetTrigger(_triggerName);
				_hasChanged = true;

				StartCoroutine(ChangeMood());

				BroAudio.Play(_escapingBGM).AsBGM().SetTransition(Transition.CrossFade, _transitionTime);
			}	
		}

		private IEnumerator ChangeMood()
		{
			_moodLight.SetActive(true);
			Vector3 originalRotation = _directionalLight.transform.eulerAngles;
			Color originalColor = _directionalLight.color;

			float t = 0f;
			while(t <= 1f)
			{
				_directionalLight.transform.eulerAngles = Vector3.Lerp(originalRotation, _targetRotation, t);
				_directionalLight.color = Color.Lerp(originalColor, _targetColor, t);
				yield return null;
				t += Time.deltaTime / (_transitionTime * 2);
			}
		}
	}
}