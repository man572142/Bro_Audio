using System.IO;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
#if UNITY_EDITOR
	public static class BroUserDataGenerator
	{
		public static void CheckAndGenerateUserData()
		{
			if (TryGetCoreData(out var coreData))
			{
				return;
			}

			string resourcePath = DefaultResoucesFolderPath;
			if (TryLoadResources<SoundManager>(nameof(SoundManager), out var soundManager))
			{
				string pathWithFileName = AssetDatabase.GetAssetPath(soundManager);
				resourcePath = Path.GetDirectoryName(pathWithFileName);
			}

			coreData = CreateCoreData(GetAssetSavePath(resourcePath, CoreDataResourcesPath));
			CreateSettingIfNotExist<EditorSetting>(GetAssetSavePath(resourcePath, EditorSettingPath));
			CreateSettingIfNotExist<RuntimeSetting>(GetAssetSavePath(resourcePath, RuntimeSettingPath));

			AssetDatabase.SaveAssets();
		}

		private static string GetAssetSavePath(string resourcesPath, string relativePath)
		{
			return Path.Combine(resourcesPath, relativePath + ".asset");
		}

		private static BroAudioData CreateCoreData(string path)
		{
			BroAudioData coreData = ScriptableObject.CreateInstance<BroAudioData>();
			coreData.AssetOutputPath = DefaultAssetOutputPath;

			#region Copy Old Core Data Value
			if (TryGetOldCoreDataTextAsset(out var oldTextAsset) && TryParseCoreData(oldTextAsset, out var oldCoreData))
			{
				coreData.AssetOutputPath = oldCoreData.AssetOutputPath;
				foreach (string guid in oldCoreData.GUIDs)
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var asset = AssetDatabase.LoadAssetAtPath<AudioAsset>(assetPath);
					coreData.AddAsset(asset);
				}

				string oldCoreDataPath = AssetDatabase.GetAssetPath(oldTextAsset);
				string fileName = Path.GetFileName(oldCoreDataPath);
				string deprecatedKeyword = "_deprecated.json";
				if (!fileName.EndsWith(deprecatedKeyword))
				{
					fileName = fileName.Replace(".json", deprecatedKeyword);
				}
				AssetDatabase.RenameAsset(oldCoreDataPath, fileName);
				EditorUtility.SetDirty(oldTextAsset);
			} 
			#endregion

			AssetDatabase.CreateAsset(coreData, path);
			return coreData;
		}

		private static void CreateSettingIfNotExist<T>(string path) where T : ScriptableObject
		{
			if (!TryLoadResources<T>(path, out _))
			{
				var setting = ScriptableObject.CreateInstance<T>();
				AssetDatabase.CreateAsset(setting, path);
				if (setting is EditorSetting editorSetting)
				{
					editorSetting.ResetToFactorySettings();
				}
				else if (setting is RuntimeSetting runtimeSetting)
				{
					runtimeSetting.ResetToFactorySettings();
				}
				EditorUtility.SetDirty(setting);
			}
		}

		private static bool TryLoadResources<T>(string path, out T resouece) where T : Object
		{
			resouece = Resources.Load<T>(path);
			return resouece != null;
		}
	}  
#endif
}