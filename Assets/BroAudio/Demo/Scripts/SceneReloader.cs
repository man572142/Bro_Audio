using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Ami.Extension;

namespace Ami.BroAudio.Demo
{
    public class SceneReloader : MonoBehaviour
    {
        [SerializeField] private Image _fadingImage = null;
        [SerializeField] private float _fadeOutTime = 2f;

        private bool _isFading = false;

        private void OnTriggerEnter(Collider other)
        {
            if (!_isFading && other.gameObject.CompareTag("Player"))
            {
                StartCoroutine(FadeOutAndReloadScene());
            }
        }

        private IEnumerator FadeOutAndReloadScene()
        {
            _isFading = true;

            AsyncOperation sceneLoader = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            sceneLoader.allowSceneActivation = false;

            yield return LerpFadeOutColor();

            sceneLoader.allowSceneActivation = true;
        }

        private IEnumerator LerpFadeOutColor()
        {
            float time = 0f;
            while (time < _fadeOutTime)
            {
                _fadingImage.color = Color.Lerp(default, Color.white, (time / _fadeOutTime).SetEase(Ease.OutCubic));
                yield return null;
                time += Time.deltaTime;
            }
            _fadingImage.color = Color.black;
        }
    }
}