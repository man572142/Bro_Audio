using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MiProduction.Extension.LoopExtension;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		// �̫�@��enum = ALL�[1�A�k���@��
		public static readonly int LastAudioType = ((int)AudioType.All + 1) >> 1;
		public const int IdMultiplier = 100; // �Ψ�1000�|�W�Xint�W���A�Y���ݭn�h�������long
		

		public static int ToConstantID(this AudioType audioType)
		{
			if (audioType == AudioType.None)
			{
				return 0;
			}

			// Faster than Math.Log2 ()
			int result = 1;
			int type = (int)audioType;

			While(_ => (type >> 1) > 0, () => 
			{
				type = type >> 1;
				result *= IdMultiplier;

				return Statement.Continue;
			});
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
				return AudioType.All;
			}
			return (AudioType)next;
		}

		public static AudioType GetAudioType(int id)
		{
			AudioType resultType = AudioType.None;
			AudioType nextType = resultType.ToNext();

			While(_ => nextType <= (AudioType)LastAudioType, () =>
			{
				if (id >= resultType.ToConstantID() && id < nextType.ToConstantID())
				{
					return Statement.Break;
				}
				resultType = nextType;
				nextType = nextType.ToNext();

				return Statement.Continue;
			});
			return resultType;
		}
		
		/// <summary>
		/// �C���j��Hcallback�^��AudioType
		/// </summary>
		public static void LoopAllAudioType(Action<AudioType> loopCallback)
		{
			AudioType currentType = AudioType.None;
			While(_ => currentType <= (AudioType)LastAudioType, () =>
			{
				loopCallback?.Invoke(currentType);
				currentType = currentType.ToNext();
				return Statement.Continue;
			});
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
