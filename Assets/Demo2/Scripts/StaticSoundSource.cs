using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class StaticSoundSource : MonoBehaviour
	{
		[SerializeField] AudioID _sound = default;

		private void Start () 
		{
			BroAudio.Play(_sound, transform.position);
		}
	}
}