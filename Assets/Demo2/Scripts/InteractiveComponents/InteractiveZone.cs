using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveZone : MonoBehaviour
{
	public event Action<bool> OnInZoneStateChanged;

	public bool IsInZone { get; private set; } = false;

	private void OnTriggerEnter(Collider other)
	{
		if(!IsInZone)
		{
			IsInZone = true; 
			OnInZoneStateChanged?.Invoke(true);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if(IsInZone)
		{
			IsInZone = false;
			OnInZoneStateChanged?.Invoke(false);
		}
	}
}
