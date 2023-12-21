using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ami.BroAudio.Demo
{
	public class CutScenePlayer : InteractiveComponent
	{
		[SerializeField] PlayableDirector _director = null;
		[SerializeField] AudioID _openingSong = default;

		protected override bool IsTriggerOnce => true;

		public override void OnInZoneChanged(bool isInZone)
		{
			base.OnInZoneChanged(isInZone);

			_director.Play();
			BroAudio.Play(_openingSong).AsBGM();
		}
	}
}