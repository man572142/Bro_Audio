using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public const string LogTitle = "<b><color=#add8e6ff>[BroAudio] </color></b>";

		public static void LogError(string message)
		{
			Debug.LogError(LogTitle + message);
		}

		public static void LogWarning(string message)
		{
			Debug.LogWarning(LogTitle + message);
		}

		public static void Log(string message)
		{
			Debug.Log(LogTitle + message);
		}
	}
}