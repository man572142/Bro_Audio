using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

namespace MiProduction.BroAudio
{
	public static class AudioJsonUtility
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

		public static void WriteJson(string assetFilePath, AudioType audioType, string[] dataToWrite, ref List<AudioData> audioData)
		{
			List<AudioData> newDataList = new List<AudioData>();

			IEnumerable<AudioData> oldDatas = null;
			IEnumerable<int> currentTypeIdList = null;

			if (audioData != null)
			{
				oldDatas = audioData.Where(x => x.AssetGUID != assetFilePath);
				if(oldDatas != null)
				{
					newDataList.AddRange(oldDatas);
					currentTypeIdList = oldDatas.Where(x => x.AudioType == audioType).Select(x => x.ID);
				}	
			}

			for (int i = 0; i < dataToWrite.Length; i++)
			{
				if (!IsValidName(dataToWrite[i]))
				{
					continue;
				}
				int id = GetUniqueID(audioType, currentTypeIdList);
				newDataList.Add(new AudioData(id, dataToWrite[i], audioType, assetFilePath));
			}
			audioData = newDataList;
			SerializedAudioDataList serializedData = new SerializedAudioDataList(newDataList);
			Debug.Log("Write : " + JsonUtility.ToJson(serializedData));
			File.WriteAllText(JsonFilePath, JsonUtility.ToJson(serializedData));
		}

		public static List<AudioData> ReadJson()
		{
			if (File.Exists(JsonFilePath))
			{
				string json = File.ReadAllText(JsonFilePath);
				Debug.Log("Read : " + json);
				SerializedAudioDataList data = JsonUtility.FromJson<SerializedAudioDataList>(json);
				return data.Datas;
			}
			else
			{
				File.WriteAllText(JsonFilePath,string.Empty);
				return null;
			}

			
		}

		private static int GetUniqueID(AudioType audioType, IEnumerable<int> idList)
		{
			int id = 0;

			while (true)
			{
				id = audioType.ToConstantID() + UnityEngine.Random.Range(0, ConstantID.TypeCapacity);
				if(idList == null || !idList.Contains(id))
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
				if (!Char.IsLetter(word))
				{
					return false;
				}
			}
			return true;
		}
	}

}