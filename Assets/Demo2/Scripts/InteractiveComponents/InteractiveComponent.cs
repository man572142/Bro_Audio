using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public abstract class InteractiveComponent : MonoBehaviour
	{
		[SerializeField] protected InteractiveZone InteractiveZone = null;

		protected virtual bool ListenToInteractiveZone() => true;
		protected virtual bool IsTriggerOnce => false;

		protected virtual void Awake()
		{
			if(ListenToInteractiveZone())
			{
				InteractiveZone.OnInZoneStateChanged += OnInZoneChanged;
			}
		}

		protected virtual void OnDestroy()
		{
			InteractiveZone.OnInZoneStateChanged -= OnInZoneChanged;
		}

		public virtual void OnInZoneChanged(bool isInZone)
		{
			if(isInZone && IsTriggerOnce)
			{
				InteractiveZone.OnInZoneStateChanged -= OnInZoneChanged;
			}
		}
	} 
}
