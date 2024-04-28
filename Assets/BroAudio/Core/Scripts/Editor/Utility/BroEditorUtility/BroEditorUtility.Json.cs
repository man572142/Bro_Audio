using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEditor;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		[Serializable]
		public class SerializedCoreData
		{
			public string AssetOutputPath;
			public List<string> GUIDs;

			public SerializedCoreData(string assetOutputPath,List<string> guids)
			{
				AssetOutputPath = assetOutputPath;
				GUIDs = guids;
            }
		}

        public static void WriteGuidToCoreData(List<string> allAssetGUID)
		{
			RewriteCoreData((coreData) => coreData.GUIDs = allAssetGUID);
        }

        public static void WriteAssetOutputPathToCoreData(string newOutputPath)
		{
            RewriteCoreData((coreData) => coreData.AssetOutputPath = newOutputPath);
		}

        public static List<string> GetGUIDListFromJson(bool showError = true)
        {
            if (TryGetCoreData(out string path, out SerializedCoreData coreData, showError))
            {
                return coreData.GUIDs;
            }
            return null;
        }

        private static void RewriteCoreData(Action<SerializedCoreData> onModifyCoreData)
        {
            if (TryGetCoreDataTextAsset(out var coreDataAsset) && TryParseCoreData(coreDataAsset, out var coreData))
            {
                onModifyCoreData?.Invoke(coreData);
				string path = AssetDatabase.GetAssetPath(coreDataAsset);
                RewriteCoreData(path, coreData);
            }
        }

        private static void RewriteCoreData(string path, SerializedCoreData coreData)
		{
            File.WriteAllText(path, JsonUtility.ToJson(coreData, true));
            AssetDatabase.Refresh();
        }

		private static bool TryGetCoreDataTextAsset(out TextAsset asset)
		{
            asset = Resources.Load<TextAsset>(CoreDataResourcesPath);
			return asset != null;
        }

		private static bool TryParseCoreData(TextAsset textAsset,out SerializedCoreData coreData)
		{
			coreData = default;
            if (textAsset != null && !string.IsNullOrEmpty(textAsset.text))
			{
                coreData = JsonUtility.FromJson<SerializedCoreData>(textAsset.text);
				return true;
            }
			return false;
		}

		public static bool TryGetCoreData(out string path,out SerializedCoreData coreData, bool showError = true)
		{
			coreData = default;
			path = null;
			if(!TryGetCoreDataTextAsset(out var textAsset) || !TryParseCoreData(textAsset, out coreData))
			{
				if(showError)
				{
                    Debug.LogError(Utility.LogTitle + "Can't find core data! please place [BroAudioData.json] in Resources folder or reinstall BroAudio");
                }
				return false;
            }
            path = AssetDatabase.GetAssetPath(textAsset);
            return true;
		}

		private static void DeleteJsonDataByAssetPath(string[] deletedAssetPaths)
		{
			var currAssetGUIDs = GetGUIDListFromJson(false);
			if(currAssetGUIDs != null)
			{
				foreach (string path in deletedAssetPaths)
				{
					if(!string.IsNullOrEmpty(path))
					{
						string guid = AssetDatabase.AssetPathToGUID(path);
						currAssetGUIDs.Remove(guid);
					}
				}

				WriteGuidToCoreData(currAssetGUIDs);
			}
		}
	}
}