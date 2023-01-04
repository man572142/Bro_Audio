using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using static MiProduction.Extension.LoopExtension;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		[Serializable]
		public struct SerializedAudioDataList
		{
			public List<AudioData> Datas;

			public SerializedAudioDataList(List<AudioData> datas)
			{
				Datas = datas;
			}
		}

		private static readonly string JsonFilePath = Application.dataPath + "/BroAudio/BroAudioData.json";

		private static void WriteJson(string assetGUID, string libraryName, string[] dataToWrite,AudioType audioType ,ref List<AudioData> allAudioData)
		{
			allAudioData?.RemoveAll(x => x.AssetGUID == assetGUID);
			IEnumerable<int> usedIdList = allAudioData?.Where(x => x.LibraryName == libraryName).Select(x => x.ID);

			for (int i = 0; i < dataToWrite.Length; i++)
			{
				if (!IsValidName(dataToWrite[i]))
				{
					continue;
				}
				int id = GetUniqueID(audioType, usedIdList);
				string name = dataToWrite[i].Replace(" ", string.Empty);
				allAudioData.Add(new AudioData(id, name, libraryName, assetGUID));
			}
			WriteToFile(allAudioData);
		}

		private static void WriteToFile(List<AudioData> audioData)
		{
			SerializedAudioDataList serializedData = new SerializedAudioDataList(audioData);
			File.WriteAllText(JsonFilePath, JsonUtility.ToJson(serializedData, true));
		}

		public static List<AudioData> ReadJson()
		{
			if (File.Exists(JsonFilePath))
			{
				string json = File.ReadAllText(JsonFilePath);
				if(string.IsNullOrEmpty(json))
				{
					return new List<AudioData>();
				}
				SerializedAudioDataList data = JsonUtility.FromJson<SerializedAudioDataList>(json);
				return data.Datas;
			}
			else
			{
				File.WriteAllText(JsonFilePath,string.Empty);
				return new List<AudioData>();
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

		private static bool IsValidName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return false;
			}

			foreach (char word in name)
			{
				if (!Char.IsLetter(word) && !Char.IsWhiteSpace(word))
				{
					return false;
				}
			}
			return true;
		}
	}

}