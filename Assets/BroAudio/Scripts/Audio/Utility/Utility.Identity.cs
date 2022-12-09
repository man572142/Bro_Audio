using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		// 最後一個enum = ALL加1再右移一位
		public static readonly int LastAudioType = ((int)AudioType.All + 1) >> 1;
		public const int IdMultiplier = 1000;
		

		public static int ToConstantID(this AudioType audioType)
		{
			if (audioType == AudioType.None)
			{
				return 0;
			}

			int result = 1;
			int type = (int)audioType;

			// Faster than Math.Log2 ()
			while ((type >> 1 ) > 0)
			{
				type = type >> 1; 
				result *= IdMultiplier;
			}
			return result;
		}

		public static AudioType ToNext(this AudioType current)
		{
			if(current == 0)
			{
				return current + 1;
			}

			int next = (int)current << 1;
			if(next > LastAudioType)
			{
				// 回傳ALL可以work但不是很漂亮
				return AudioType.All;
			}
			return (AudioType)next;
		}

		public static AudioType GetAudioType(int id)
		{
			AudioType resultType = AudioType.None;
			AudioType nextType = resultType.ToNext();

			while(nextType <= (AudioType)LastAudioType)
			{
				if(id >= resultType.ToConstantID() && id < nextType.ToConstantID())
				{
					break;
				}
				resultType = nextType;
				nextType = nextType.ToNext();
			}
			return resultType;
		}

		//public static bool TryGetAudioIDRange(AudioType audioType,out int min,out int max)
		//{
		//	min = -1;
		//	max = -1;
			
		//	return true;
		//}

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
