using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using static MiProduction.Extension.LoopExtension;
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
			public List<AudioData> Datas;

			public SerializedCoreData(string rootPath,string enumsPath,List<AudioData> datas)
			{
				RootPath = rootPath;
				EnumsPath = enumsPath;
				Datas = datas;
			}

			public SerializedCoreData(List<AudioData> datas)
			{
				RootPath = Utility.RootPath;
				EnumsPath = Utility.EnumsPath;
				Datas = datas;
			}
		}

		private static void WriteJson(string assetGUID, string libraryName, string[] dataToWrite,AudioType audioType ,List<AudioData> allAudioData,Action onAudioDataUpdatFinished)
		{
			allAudioData?.RemoveAll(x => x.AssetGUID == assetGUID);
			IEnumerable<int> usedIdList = allAudioData?.Where(x => x.LibraryName == libraryName).Select(x => x.ID);

			for (int i = 0; i < dataToWrite.Length; i++)
			{
				if (!IsValidName(dataToWrite[i],out ValidationErrorCode errorCode))
				{
					continue;
				}
				int id = GetUniqueID(audioType, usedIdList);
				string name = dataToWrite[i].Replace(" ", string.Empty);
				allAudioData.Add(new AudioData(id, name, libraryName, assetGUID));
			}
			onAudioDataUpdatFinished?.Invoke();
			WriteToFile(allAudioData);
		}

		private static void WriteEmptyAudioData(string assetGUID, string libraryName, ref List<AudioData> allAudioData)
		{
			allAudioData.Add(new AudioData(0, "None", libraryName,assetGUID));
			WriteToFile(allAudioData);
		}

		private static void WriteToFile(List<AudioData> audioDatas)
		{
			SerializedCoreData serializedData = new SerializedCoreData(RootPath,EnumsPath,audioDatas);
			File.WriteAllText(GetFilePath(RootPath,CoreDataFileName), JsonUtility.ToJson(serializedData, true));
		}

		public static void CreateDefaultCoreData()
		{
			WriteToFile(null);
		}

		public static List<AudioData> ReadJson()
		{
			string coreDataFilePath = GetFilePath(RootPath, CoreDataFileName);
			if (File.Exists(coreDataFilePath))
			{
				string json = File.ReadAllText(coreDataFilePath);
				if(string.IsNullOrEmpty(json))
				{
					return new List<AudioData>();
				}
				SerializedCoreData data = JsonUtility.FromJson<SerializedCoreData>(json);
				return data.Datas;
			}
			else
			{
				//BroAudioEditorWindow.ShowWindow();
				//FindJsonData();

				Debug.Log("NoData");


				return null;
				//File.WriteAllText(JsonFileDir.FilePath, string.Empty);
				//return new List<AudioData>();
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

		private static void DeleteJsonDataByAsset(string assetGUID,out List<AudioData> currentAudioDatas,out string deletedLibraryName)
		{
			currentAudioDatas = ReadJson();
			deletedLibraryName = string.Empty;

			int dataCount = currentAudioDatas != null? currentAudioDatas.Count :0 ;
			for (int i = dataCount - 1; i >= 0 ; i--)
			{
				if(currentAudioDatas[i].AssetGUID == assetGUID)
				{
					deletedLibraryName = currentAudioDatas[i].LibraryName;
					currentAudioDatas.RemoveAt(i);
				}
			}

			if(!string.IsNullOrEmpty(deletedLibraryName))
			{
				WriteToFile(currentAudioDatas);
			}
		}

		private static int GetUniqueID(AudioType audioType, IEnumerable<int> idList)
		{
			int id = 0;

			Loop(() =>
			{
				id = UnityEngine.Random.Range(audioType.ToConstantID(), audioType.ToNext().ToConstantID());
				if (idList == null || !idList.Contains(id))
				{
					return Statement.Break;
				}
				return Statement.Continue;
			});
			return id;
		}
	}

}