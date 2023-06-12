using System.Collections.Generic;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static string ToName(this int id)
		{
			return SoundManager.Instance.GetNameByID(id);
		}

		public static T CastTo<T>(this IAudioLibrary library) where T : IAudioLibrary
		{
			return (T)library;
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
	}
}