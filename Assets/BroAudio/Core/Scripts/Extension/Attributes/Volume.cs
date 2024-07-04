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
#if UNITY_WEBGL
			CanBoost = false; 
#else
			CanBoost = true;
#endif
		}

		public Volume(bool canBoost)
		{
#if UNITY_WEBGL
			Debug.LogWarning(Utility.LogTitle + "Volume boosting is not supported in WebGL");
			CanBoost = false;
#else
			CanBoost = canBoost;
#endif
		}
	} 
}
