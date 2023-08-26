using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;
using static Ami.BroAudio.BroLog;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		[Serializable]
		public class SerializedCoreData
		{
			public string AssetOutputPath;
			public List<string> GUIDs;
			public List<AudioTypeLastID> AudioTypeLastIDs;

			public SerializedCoreData(string assetOutputPath,List<string> guids,List<AudioTypeLastID> lastIDs)
			{
				AssetOutputPath = assetOutputPath;
				GUIDs = guids;
				AudioTypeLastIDs = lastIDs;
            }
		}

		[Serializable]
		public struct AudioTypeLastID
		{
			public BroAudioType AudioType;
			public int LastID;
		}

        //[MenuItem("BroAudio/[DevOnly]Reset All Audio Types Last ID")]
        public static void ResetAllAudioTypeLastID()
        {
			var idList = CreateLastIDs();
			WriteLastIDsToCoreData(idList);
        }

        public static void WriteGuidToCoreData(List<string> allLibraryGUID)
		{
			RewriteCoreData((coreData) => coreData.GUIDs = allLibraryGUID);
        }

        public static void WriteAssetOutputPathToCoreData(string newOutputPath)
		{
            RewriteCoreData((coreData) => coreData.AssetOutputPath = newOutputPath);
		}

		public static void WriteLastIDsToCoreData(List<AudioTypeLastID> lastIDs)
		{
            RewriteCoreData((coreData) => coreData.AudioTypeLastIDs = lastIDs);
        }

        public static void RewriteCoreData(Action<SerializedCoreData> onModifyCoreData)
        {
            if (TryGetCoreDataTextAsset(out var coreDataAsset) && TryParseCoreData(coreDataAsset, out var coreData))
            {
                onModifyCoreData?.Invoke(coreData);
                RewriteCoreData(coreDataAsset, coreData);
            }
        }

        public static void RewriteCoreData(TextAsset coreDataAsset, SerializedCoreData coreData)
		{
            string path = AssetDatabase.GetAssetPath(coreDataAsset);
            File.WriteAllText(path, JsonUtility.ToJson(coreData, true));
            AssetDatabase.Refresh();
        }

        public static List<AudioTypeLastID> CreateLastIDs()
        {
            List<AudioTypeLastID> idList = new List<AudioTypeLastID>();
            ForeachConcreteAudioType((audioType) =>
            {
                var idPair = new AudioTypeLastID() { AudioType = audioType, LastID = audioType.GetInitialID() };
                idList.Add(idPair);
            });
			return idList;
        }

        public static List<string> GetGUIDListFromJson()
		{
			if(TryGetCoreData(out SerializedCoreData coreData))
			{
				return coreData.GUIDs;
			}
			return null;
		}

		public static bool TryGetCoreDataTextAsset(out TextAsset asset)
		{
            asset = Resources.Load<TextAsset>(CoreDataResourcesPath);
			return asset != null;
        }

		public static bool TryParseCoreData(TextAsset textAsset,out SerializedCoreData coreData)
		{
			coreData = default;
            if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
			{
                coreData = JsonUtility.FromJson<SerializedCoreData>(textAsset.text);
				return true;
            }
			return false;
		}

		public static bool TryGetCoreData(out SerializedCoreData coreData)
		{
			coreData = default;
			if(!TryGetCoreDataTextAsset(out var textAsset) || !TryParseCoreData(textAsset, out coreData))
			{
                LogError("Can't find core data! please place [BroAudioData.json] in Resources folder or reinstall BroAudio");
				return false;
            }
			return true;
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