using MiProduction.BroAudio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(Button))]
public class ButtonSoundTrigger : MonoBehaviour,IPointerClickHandler,IPointerDownHandler,IPointerEnterHandler,IPointerExitHandler,IPointerUpHandler
{
    [SerializeField] Button _button = null;

#pragma warning disable CS0414
	[SerializeField] UnityEvent _onClick = null;
	[SerializeField] UnityEvent _onPointerDown = null;
	[SerializeField] UnityEvent _onPointerEnter = null;
	[SerializeField] UnityEvent _onPointerExit = null;
	[SerializeField] UnityEvent _onPointerUp = null;
# pragma warning restore CS0414

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
        //BroAudio.PlaySound(_uiSound);
    }

	public void OnPointerClick(PointerEventData eventData)
	{
		_button.OnPointerClick(eventData);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		throw new NotImplementedException();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		throw new NotImplementedException();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		throw new NotImplementedException();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		throw new NotImplementedException();
	}
}
