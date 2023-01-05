using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class BroAudioButton : Button
{
	[SerializeField] UnityEvent[] unityEvents;

	public override void OnPointerClick(PointerEventData eventData)
	{
		base.OnPointerClick(eventData);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
	}
	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
	}
}
