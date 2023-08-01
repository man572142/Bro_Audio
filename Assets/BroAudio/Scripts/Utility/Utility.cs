using System.Collections.Generic;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.Runtime;
using static MiProduction.BroAudio.BroLog;

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
			BroAudioType audioType = GetAudioType(library.ID);
			AudioLibrary audioLibrary = library as AudioLibrary;
			if(audioLibrary.PossibleFlags.HasFlag(audioType))
			{
				return (T)library;
			}
			else
			{
				LogError($"The library of {library.Name} can't cast to {typeof(T)}");
				return default;
			}
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