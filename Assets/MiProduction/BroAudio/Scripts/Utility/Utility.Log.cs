using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Runtime;
using System;

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

		//public static string BroMessage(ErrorCode errorCode,Color color = default,params object[] args)
		//{
		//	string errorString = GetErrorString(errorCode);

			
		//}

		//private static string GetErrorString(ErrorCode errorCode)
		//{
		//	switch (errorCode)
		//	{
		//		case ErrorCode.ObjectReferenceIsNull:
		//			return ""
		//		case ErrorCode.ElementAtIndexIsNull:
		//			break;
		//		case ErrorCode.InvalidValue:
		//			break;
		//		case ErrorCode.DataEmpty:
		//			break;
		//	}
		//}
	}


	//public enum ErrorCode
	//{
	//	ObjectReferenceIsNull,
	//	ElementAtIndexIsNull,
	//	InvalidValue,
	//	DataEmpty,
	//	ValueEmpty,
	//	InitializeFailed,
	//	NotInitializeYet,
	//}
}