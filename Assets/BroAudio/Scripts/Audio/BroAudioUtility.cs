using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static class BroAudioUtility
	{
		public static int ToConstantID(this AudioType audioType)
		{
			switch (audioType)
			{
				case AudioType.None:
					return 0;
				case AudioType.SFX:
					return ConstantID.Sfx;
				case AudioType.Music:
					return ConstantID.Music;
				case AudioType.Ambience:
					return ConstantID.Sfx;
				case AudioType.UI:
					return ConstantID.UI;
				case AudioType.StandOut:
					return ConstantID.Sfx;
				case AudioType.All:
				default:
					Debug.LogError($"{audioType} doesn't have a ConstantID");
					return -1;
			}
		}

		public static Type GetEnumType(string enumName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(enumName);
				if (type == null)
					continue;
				if (type.IsEnum)
					return type;
			}
			return null;
		}

	} 
}
