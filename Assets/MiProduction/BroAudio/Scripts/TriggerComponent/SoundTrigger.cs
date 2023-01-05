using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiProduction.BroAudio
{
	public class SoundTrigger : MonoBehaviour
	{
		[SerializeField] TriggerEvent _triggerEvent;

		[SerializeField] UnityEvent[] _events = null;


		public void OnTriggerEnter(Collider other)
		{
			if(_triggerEvent.Contains(TriggerEvent.OnTriggerEnter))
			{

			}
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			
		}

		private void OnTriggerExit(Collider other)
		{
			
		}

		private void OnTriggerExit2D(Collider2D collision)
		{
			
		}

		private void OnCollisionEnter(Collision collision)
		{
			
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			
		}
	} 
}
