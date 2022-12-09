using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Core;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public const string LogTitle = "<b><color=#add8e6ff>[BroAudio] </color></b>";

		public static void LogError(string message)
		{
# if UNITY_EDITOR
			Debug.LogError(LogTitle + message);
#endif
		}

		public static void LogWarning(string message)
		{
#if UNITY_EDITOR
			Debug.LogWarning(LogTitle + message);
#endif
		}

		public static void Log(string message)
		{
#if UNITY_EDITOR
			Debug.Log(LogTitle + message);
#endif
		}
	}


	//public enum ErrorCode
	//{
	//	NoSuchMusicData,

	//}
}