using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ami.BroAudio.Demo
{
	public class ThemeChangeZone : InteractiveComponent
	{
		[SerializeField] SoundID _bgm = default;
		[SerializeField] Transition _transition = default;
		[SerializeField] float _transitionTime = default;
        [SerializeField] float _sceneFadeOutTime = 2f;

		[Header("Change Mood")]
		[SerializeField] Animator _lightAnimator = null;
		[SerializeField] string _ligghtAnimParameterName = null;
        [SerializeField] GameObject _reloadSceneRoad = null;

		private bool _isNightTime = false;
		private bool _canChange = true;

        protected override void Awake()
        {
            base.Awake();
            SceneManager.activeSceneChanged += OnSceneChanged;

            _reloadSceneRoad.SetActive(_isNightTime);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

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
            _reloadSceneRoad.SetActive(_isNightTime);
        }

		private IEnumerator PreventChangePeriod()
		{
			_canChange = false;
			yield return new WaitForSeconds(_transitionTime);
			_canChange = true;
		}

        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            BroAudio.Stop(_bgm, _sceneFadeOutTime);
        }
    }
}