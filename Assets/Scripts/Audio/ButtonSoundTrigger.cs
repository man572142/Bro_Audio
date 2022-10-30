using MiProduction.BroAudio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSoundTrigger : MonoBehaviour
{
    [SerializeField] Button _button = null;
    [SerializeField] UI _uiSound = UI.None;

    private void Start()
    {
        if(_button == null)
        {
            _button = GetComponent<Button>();
        }

        _button.onClick.AddListener(OnButtonClick);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        SoundSystem.PlaySFX(_uiSound);
    }
}
