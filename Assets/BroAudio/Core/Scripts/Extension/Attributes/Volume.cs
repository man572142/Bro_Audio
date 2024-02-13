using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
	public class Volume : PropertyAttribute
	{
		public bool CanBoost = false;

		public Volume()
		{
			CanBoost = false;
		}

		public Volume(bool canBoost)
		{
			CanBoost = canBoost;
		}
	} 
}
