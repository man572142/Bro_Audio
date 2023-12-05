using System.Collections.Generic;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
		public static string ToName(this int id)
		{
			return SoundManager.Instance.GetNameByID(id);
		}

		public static T DecorateWith<T>(this AudioPlayer origin) where T : AudioPlayerDecorator, new()
		{
			if (origin != null)
			{
				T result = new T();
				result.Init(origin);
				return result;
			}
			return null;
		}

		public static bool Contains(this RandomFlags flags, RandomFlags targetFlag)
		{
			// faster than Enum.HasFlag, could be used in runtime.
			return ((int)flags & (int)targetFlag) == 1;
		}
	}
}