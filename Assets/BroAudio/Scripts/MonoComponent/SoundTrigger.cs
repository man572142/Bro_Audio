using Ami.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
	[AddComponentMenu("BroAudio/" + nameof(SoundTrigger))]
	public class SoundTrigger : MonoBehaviour
	{
        [SerializeField] TriggerData[] _triggers;

		private void Trigger(UnityMessage triggerEvent)
		{

		}

		private void Awake()
		{
			Trigger(UnityMessage.Awake);
		}

		private void Start()
		{
			Trigger(UnityMessage.Start);
		}


		private void FixedUpdate()
		{
			Trigger(UnityMessage.FixedUpdate);
		}

		private void Update()
		{
			Trigger(UnityMessage.Update);
		}

		private void OnTriggerEnter(Collider other)
		{

		}

		private void OnCollisionEnter(Collision collision)
		{

		}



		public static class NameOf
		{
			public static string Triggers => nameof(_triggers);
		}
	}
}