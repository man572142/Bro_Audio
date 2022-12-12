using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public const string DefaultEnumsPath = "Assets/BroAudio/Scripts/Audio/Enums";
		public static void WriteAudioData(string assetGUID,AudioType audioType,string[] dataToWrite,out List<AudioData> currentAudioDatas)
		{
			currentAudioDatas = ReadJson();
			WriteJson(assetGUID, audioType, dataToWrite, ref currentAudioDatas);
			GenerateEnum(audioType, currentAudioDatas);
		}

		public static void DeleteAudioData(string assetGUID)
		{
			DeleteJsonDataByAsset(assetGUID,out var currentAudioDatas,out var deletedType);
			if(deletedType != AudioType.None)
			{
				GenerateEnum(deletedType, currentAudioDatas);
			}
			
		}
	}

}