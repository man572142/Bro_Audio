using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
    public class CannonUI : MonoBehaviour
    {
        [SerializeField] Cannon _controller = null;

        [SerializeField] Text _apiTextComponent = null;
        [SerializeField] Slider _progressBar = null;

        private string _apiText = null;

        private void Awake ()
        {
            _controller.OnForceChanged += UpdateForceUI;
            _controller.OnFire += CloseUI;
            _controller.OnReloaded += OpenUI;

            _progressBar.maxValue = _controller.MaxForce;
            _progressBar.value = _progressBar.minValue;
            _apiText = _apiTextComponent.text;
        }

        private void OnDestroy()
        {
            _controller.OnForceChanged -= UpdateForceUI;
            _controller.OnFire -= CloseUI;
            _controller.OnReloaded -= OpenUI;
        }

        private void UpdateForceUI(float force)
        {
            _apiTextComponent.text = string.Format(_apiText, (int)force);
            _progressBar.value = force;
        }

        private void CloseUI(float force)
        {
            _apiTextComponent.text = string.Format(_apiText, (int)force);
            gameObject.SetActive(false);
        }

        private void OpenUI()
        {
            gameObject.SetActive(true);
        }
    }
}