using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ami.BroAudio.Demo
{
	public class CutScenePlayer : MonoBehaviour
	{
		[SerializeField] InteractiveZone _interactiveZone = null;
		[SerializeField] PlayableDirector _director = null;
		[SerializeField] AudioID _openingSong = default;

		private void Awake()
		{
			_interactiveZone.OnInZoneStateChanged += PlayCutScene;
		}

		private void PlayCutScene(bool isInZone)
		{
			if(!isInZone)
			{
				return;
			}

			_interactiveZone.OnInZoneStateChanged -= PlayCutScene;
			_director.Play();
			BroAudio.Play(_openingSong).AsBGM();
		}
	}
}