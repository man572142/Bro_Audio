using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using static MiProduction.BroAudio.BroLog;

namespace MiProduction.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		[Serializable]
		public struct SerializedCoreData
		{
			public string AssetOutputPath;
			public List<string> GUIDs;

			public SerializedCoreData(string assetOutputPath,List<string> guids)
			{
				AssetOutputPath = assetOutputPath;
				GUIDs = guids;
			}
		}

		public static void WriteGuidToCoreData(List<string> allLibraryGUID)
		{
			var coreFile = Resources.Load<TextAsset>(CoreDataResourcesPath);
			if(coreFile != null)
			{
				string path = AssetDatabase.GetAssetPath(coreFile);
				SerializedCoreData serializedData = new SerializedCoreData(AssetOutputPath, allLibraryGUID);
				File.WriteAllText(path, JsonUtility.ToJson(serializedData, true));
				AssetDatabase.Refresh();
			}
		}

		public static void WriteAssetOutputPathToCoreData()
		{
			var coreFile = Resources.Load<TextAsset>(CoreDataResourcesPath);
			SerializedCoreData coreData;
			if(!string.IsNullOrEmpty(coreFile.text))
			{
				coreData = JsonUtility.FromJson<SerializedCoreData>(coreFile.text);
				coreData.AssetOutputPath = AssetOutputPath;
				string path = AssetDatabase.GetAssetPath(coreFile);
				File.WriteAllText(path, JsonUtility.ToJson(coreData, true));
				AssetDatabase.Refresh();
			}
		}

		public static void CreateDefaultCoreData()
		{
			WriteGuidToCoreData(new List<string>());
		}

		public static List<string> GetGUIDListFromJson()
		{
			if(TryGetCoreData(out SerializedCoreData coreData))
			{
				return coreData.GUIDs;
			}
			return null;
		}

		public static bool TryGetCoreData(out SerializedCoreData coreData)
		{
			coreData = default;
			TextAsset textAsset = Resources.Load<TextAsset>(CoreDataResourcesPath);
			if(textAsset != null)
			{
				string json = textAsset.text;
				if (!string.IsNullOrEmpty(json))
				{
					coreData = JsonUtility.FromJson<SerializedCoreData>(json);
					return true;
				}
			}
			
			LogError("Can't find core data! please place [BroAudioData.json] in Resources folder or reinstall BroAudio");
			return false;
		}

		private static bool DeleteJsonDataByAsset(string assetGUID)
		{
			// TODO: 這裡做了兩次Resources.Load
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
				WriteGuidToCoreData(currentLibraryGUID);
			}
			return hasRemoved;
		}
	}
}