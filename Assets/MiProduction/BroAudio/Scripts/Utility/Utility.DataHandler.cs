using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static void WriteAudioData(string assetGUID,string libraryName, string[] dataToWrite,AudioType audioType,out List<AudioData> currentAudioDatas)
		{
			currentAudioDatas = ReadJson();
			WriteJson(assetGUID, libraryName, dataToWrite, audioType, ref currentAudioDatas);
			GenerateEnum(libraryName, currentAudioDatas);
		}

		public static void DeleteAudioData(string assetGUID)
		{
			DeleteJsonDataByAsset(assetGUID,out var currentAudioDatas,out var deletedLibrary);
			if(!string.IsNullOrEmpty(deletedLibrary))
			{
				GenerateEnum(deletedLibrary, currentAudioDatas);
			}	
		}
	}
}