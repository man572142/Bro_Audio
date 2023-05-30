using System.Collections.Generic;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static string ToName(this int id)
		{
			return SoundManager.Instance.GetNameByID(id);
		}
	}
}