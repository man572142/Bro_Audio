using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class DynamicSoundSource : MonoBehaviour
	{
		[SerializeField] AudioID _sound = default;
		void Start()
		{
			BroAudio.Play(_sound, transform);
		}
	} 
}
