using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static void WriteAudioData(string assetGUID,string libraryName, string[] dataToWrite,AudioType audioType,List<AudioData> currentAudioDatas,Action onAudioDataUpdatFinished)
		{
			WriteJson(assetGUID, libraryName, dataToWrite, audioType, currentAudioDatas,onAudioDataUpdatFinished);
			WriteEnum(libraryName, currentAudioDatas);
		}

		public static void DeleteLibrary(string assetGUID)
		{
			DeleteJsonDataByAsset(assetGUID,out var currentAudioDatas,out var deletedLibrary);
			if(!string.IsNullOrEmpty(deletedLibrary))
			{
				WriteEnum(deletedLibrary, currentAudioDatas);
			}	

		}

		public static void CreateNewLibrary(string assetGUID, string libraryName,List<AudioData> currentAudioDatas)
		{
			WriteEmptyAudioData(assetGUID,libraryName,ref currentAudioDatas);
			AssetDatabase.Refresh();
		}
	}
}