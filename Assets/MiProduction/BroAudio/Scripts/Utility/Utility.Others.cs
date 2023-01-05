using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static bool Contains(this TriggerEvent flags, TriggerEvent target)
		{
			return (flags & target) != 0;
		}
	}

}