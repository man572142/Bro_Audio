using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
	public struct PlaybackPreference
	{
		public readonly bool IsLoop;
		public readonly float Delay;

		public PlaybackPreference(bool isLoop, float delay)
		{
			IsLoop = isLoop;
			Delay = delay;
		}

		public PlaybackPreference(bool isLoop)
		{
			IsLoop = isLoop;
			Delay = 0f;
		}

		public PlaybackPreference(float delay)
		{
			IsLoop = false;
			Delay = delay;
		}
	}

}