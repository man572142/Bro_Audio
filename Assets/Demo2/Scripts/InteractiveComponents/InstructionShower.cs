using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	[RequireComponent(typeof(InteractiveZone))]
	public class InstructionShower : MonoBehaviour
	{
		[SerializeField] InteractiveZone _interactiveZone = null;
		[SerializeField] Animation _target = null;

		private void Awake()
		{
			_target.gameObject.SetActive(false);
			_interactiveZone.OnInZoneStateChanged += ShowInstruction;
		}

		private void OnDestroy()
		{
			_interactiveZone.OnInZoneStateChanged -= ShowInstruction;
		}

		private void ShowInstruction(bool isInZone)
		{
			_target.gameObject.SetActive(isInZone);
		}
	}
} 