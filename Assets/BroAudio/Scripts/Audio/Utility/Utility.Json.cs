using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

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

		public static void WriteJson(string assetGUID, AudioType audioType, string[] dataToWrite, ref List<AudioData> audioData)
		{
			audioData?.RemoveAll(x => x.AssetGUID == assetGUID);
			IEnumerable<int> usedIdList = audioData?.Where(x => x.AudioType == audioType).Select(x => x.ID);

			for (int i = 0; i < dataToWrite.Length; i++)
			{
				if (!IsValidName(dataToWrite[i]))
				{
					continue;
				}
				int id = GetUniqueID(audioType, usedIdList);
				string name = dataToWrite[i].Replace(" ", string.Empty);
				audioData.Add(new AudioData(id,name , audioType, assetGUID));
			}
			SerializedAudioDataList serializedData = new SerializedAudioDataList(audioData);
			File.WriteAllText(JsonFilePath, JsonUtility.ToJson(serializedData,true));
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

		private static int GetUniqueID(AudioType audioType, IEnumerable<int> idList)
		{
			int id = 0;
			while (true)
			{
				id = UnityEngine.Random.Range(audioType.ToConstantID(), audioType.ToNext().ToConstantID());
				if (idList == null || !idList.Contains(id))
				{
					return id;
				}
			}
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