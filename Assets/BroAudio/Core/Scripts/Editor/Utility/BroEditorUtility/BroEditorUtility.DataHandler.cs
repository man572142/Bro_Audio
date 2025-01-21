using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using System.Collections.Generic;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
        public static bool TryLoadResources<T>(string path, out T resouece) where T : UnityEngine.Object
        {
            resouece = Resources.Load<T>(path);
            return resouece != null;
        }

        public static bool TryGetCoreData(out BroAudioData coreData)
		{
			coreData = Resources.Load<BroAudioData>(CoreDataResourcesPath);
			return coreData;
		}

		public static bool TryGetAssetByGUID(string assetGUID,out IAudioAsset asset)
		{
			return TryGetAssetByPath(AssetDatabase.GUIDToAssetPath(assetGUID) ,out asset);
		}

		public static bool TryGetAssetByPath(string assetPath, out IAudioAsset asset)
		{
			asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IAudioAsset;
			return asset != null;
		}

        public static T CreateScriptableObjectIfNotExist<T>(string path) where T : ScriptableObject
        {
            T setting;
            if (!TryLoadResources<T>(path, out setting))
            {
                setting = ScriptableObject.CreateInstance<T>();
                if (setting is EditorSetting editorSetting)
                {
                    editorSetting.ResetToFactorySettings();
                }
                else if (setting is RuntimeSetting runtimeSetting)
                {
                    runtimeSetting.ResetToFactorySettings();
                }
                AssetDatabase.CreateAsset(setting, path);
                EditorUtility.SetDirty(setting);
            }
            return setting;
        }

        public static void WriteAssetOutputPathToSetting(string path)
		{
			EditorSetting.AssetOutputPath = path;
			SaveToDisk(EditorSetting);
		}

		public static void AddNewAssetToCoreData(ScriptableObject asset)
		{
			if(TryGetCoreData(out var coreData))
			{
				coreData.AddAsset(asset as AudioAsset);
				SaveToDisk(coreData);
			}
		}

		public static void RemoveEmptyDatas()
		{
			if (TryGetCoreData(out BroAudioData coreData))
			{
				coreData.RemoveEmpty();
				SaveToDisk(coreData);
			}
		}

		public static void ReorderAssets(List<string> _allAssetGUIDs)
		{
			if (TryGetCoreData(out var coreData))
			{
				coreData.ReorderAssets(_allAssetGUIDs);
				SaveToDisk(coreData);
			}
		}

		public static void SaveToDisk(UnityEngine.Object obj)
		{
			EditorUtility.SetDirty(obj);
			AssetDatabase.SaveAssets();
		}
	}
}