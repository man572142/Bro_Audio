using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	[RequireComponent(typeof(Collider))]
	public class InteractiveZone : MonoBehaviour
	{
		public event Action<bool> OnInZoneStateChanged;

		public bool IsInZone { get; private set; } = false;
		public GameObject InZoneObject { get; private set; }

		private void OnTriggerEnter(Collider other)
		{
			if (!IsInZone && other.gameObject.CompareTag("Player"))
			{
				IsInZone = true;
				InZoneObject = other.gameObject;
				OnInZoneStateChanged?.Invoke(true);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (IsInZone && other.gameObject.CompareTag("Player"))
			{
				IsInZone = false;
				InZoneObject = null;
				OnInZoneStateChanged?.Invoke(false);
			}
		}
	}

}