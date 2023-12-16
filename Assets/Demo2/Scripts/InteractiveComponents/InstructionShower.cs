using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class InstructionShower : InteractiveComponent
	{
		[SerializeField] Animation _instruction = null;
		[SerializeField] bool _lookAtPlayer = false;

		private Transform _instTransform = null;

		protected override bool ListenToInteractiveZone() => true;

		protected override void Awake()
		{
			base.Awake();
			_instruction.gameObject.SetActive(false);
		}

		private void Update()
		{
			if(_lookAtPlayer && InteractiveZone.IsInZone && InteractiveZone.InZoneObject)
			{
                _instTransform = _instTransform ?? _instruction.transform;

				Vector3 playerPos = InteractiveZone.InZoneObject.transform.position;
				Vector3 opppsitePos = _instTransform.position + (_instTransform.position - playerPos);
				_instTransform.LookAt(opppsitePos, Vector3.up);
			}
		}

		public override void OnInZoneChanged(bool isInZone)
		{
			_instruction.gameObject.SetActive(isInZone);
		}
	}
} 