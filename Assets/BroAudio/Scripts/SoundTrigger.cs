using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
	public class SoundTrigger : MonoBehaviour
	{

		public const string Header_Entity = "Entity";
		[SerializeField] AudioID _sound = default;

		public const string Header_TriggerSettings = "Trigger Settings";
		[SerializeField] TriggerData[] _triggerSettings = default;

		public const string Header_Environment = "Environment";

		private void Trigger(SoundTriggerEvent triggerEvent)
		{

		}

		private void Awake()
		{
			Trigger(SoundTriggerEvent.Awake);
		}

		private void Start()
		{
			Trigger(SoundTriggerEvent.Start);
		}


		private void FixedUpdate()
		{
			Trigger(SoundTriggerEvent.FixedUpdate);
		}

		private void Update()
		{
			Trigger(SoundTriggerEvent.Update);
		}

		private void OnTriggerEnter(Collider other)
		{

		}

		private void OnCollisionEnter(Collision collision)
		{

		}



		public static class NameOf
		{
			public static string Sound => nameof(_sound);
			public static string TriggerSettings => nameof(_triggerSettings);
		}
	}
}