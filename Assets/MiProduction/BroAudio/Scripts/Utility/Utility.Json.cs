using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		[Serializable]
		public struct SerializedCoreData
		{
			public string RootPath;
			public string EnumsPath;
			public List<string> GUIDs;

			public SerializedCoreData(string rootPath,string enumsPath,List<string> guids)
			{
				RootPath = rootPath;
				EnumsPath = enumsPath;
				GUIDs = guids;
			}

			public SerializedCoreData(List<string> guids)
			{
				RootPath = Utility.RootPath;
				EnumsPath = Utility.EnumsPath;
				GUIDs = guids;
			}
		}


		public static void WriteJsonToFile(List<string> allLibraryGUID)
		{
			SerializedCoreData serializedData = new SerializedCoreData(RootPath,EnumsPath,allLibraryGUID);
			File.WriteAllText(GetFilePath(RootPath,CoreDataFileName), JsonUtility.ToJson(serializedData, true));
			AssetDatabase.Refresh();
		}

		public static void CreateDefaultCoreData()
		{
			WriteJsonToFile(null);
		}

		public static List<string> GetGUIDListFromJson()
		{
			string coreDataFilePath = GetFilePath(RootPath, CoreDataFileName);
			if (File.Exists(coreDataFilePath))
			{
				string json = File.ReadAllText(coreDataFilePath);
				if(string.IsNullOrEmpty(json))
				{
					return new List<string>();
				}
				SerializedCoreData data = JsonUtility.FromJson<SerializedCoreData>(json);
				return data.GUIDs;
			}
			else
			{
				LogError("NoData!!");
				return null;
			}
		}

		private static void FindJsonData()
		{
			string[] assets = AssetDatabase.FindAssets("BroAudioData");
			foreach(string guid in assets)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
			}
		}

		private static bool DeleteJsonDataByAsset(string assetGUID)
		{
			var currentLibraryGUID = GetGUIDListFromJson();
			if(currentLibraryGUID == null)
			{
				return false;
			}
			
			bool hasRemoved = false;
			for (int i = currentLibraryGUID.Count - 1; i >= 0 ; i--)
			{
				if(currentLibraryGUID[i] == assetGUID)
				{
					currentLibraryGUID.RemoveAt(i);
					hasRemoved = true;
				}
			}
			if(hasRemoved)
			{
				WriteJsonToFile(currentLibraryGUID);
			}
			return hasRemoved;
		}

		
	}

}