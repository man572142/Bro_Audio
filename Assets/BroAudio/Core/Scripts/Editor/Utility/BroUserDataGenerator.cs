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
		private static bool _isGenerating = false;

		public static void CheckAndGenerateUserData()
		{
			if (_isGenerating || TryGetCoreData(out var coreData))
			{
				return;
			}
			_isGenerating = true;

			string resourcePath = DefaultResoucesFolderPath;
			if (!TryLoadResources<SoundManager>(nameof(SoundManager), out var soundManager))
			{
				Debug.LogError(Utility.LogTitle + $"{nameof(SoundManager)} is missing, please import it and place it in the Resources folder");
				return;
			}
			resourcePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(soundManager));

			coreData = CreateCoreData(GetAssetSavePath(resourcePath, CoreDataResourcesPath));
			AssignCoreData(soundManager, coreData);

			CreateSettingIfNotExist<EditorSetting>(GetAssetSavePath(resourcePath, EditorSettingPath));
			CreateSettingIfNotExist<RuntimeSetting>(GetAssetSavePath(resourcePath, RuntimeSettingPath));

			AssetDatabase.importPackageCompleted -= OnPackageImportComplete;
			AssetDatabase.importPackageCompleted += OnPackageImportComplete;
		}

		private static void OnPackageImportComplete(string packageName)
		{
			AssetDatabase.importPackageCompleted -= OnPackageImportComplete;
			AssetDatabase.SaveAssets();
			_isGenerating = false;
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

		private static void AssignCoreData(SoundManager soundManager, BroAudioData coreData)
		{
			soundManager.AssignCoreData(coreData);
			PrefabUtility.SavePrefabAsset(soundManager.gameObject);
		}

		private static void CreateSettingIfNotExist<T>(string path) where T : ScriptableObject
		{
			if (!TryLoadResources<T>(path, out _))
			{
				var setting = ScriptableObject.CreateInstance<T>();
				if (setting is EditorSetting editorSetting)
				{
					editorSetting.ResetToFactorySettings();
				}
				else if (setting is RuntimeSetting runtimeSetting)
				{
					runtimeSetting.ResetToFactorySettings();
				}
				AssetDatabase.CreateAsset(setting, path);
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