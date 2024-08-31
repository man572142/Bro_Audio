using System.IO;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Editor.BroEditorUtility;
using System;

namespace Ami.BroAudio.Editor
{
#if UNITY_EDITOR
	public static class BroUserDataGenerator
	{
		private static bool _isGenerating = false;

		public static void CheckAndGenerateUserData()
		{
			if (_isGenerating)
			{
				return;
			}
			_isGenerating = true;

            var request = Resources.LoadAsync<SoundManager>(nameof(SoundManager));
            request.completed += OnGetSoundManager;

            void OnGetSoundManager(AsyncOperation operation)
            {
                request.completed -= OnGetSoundManager;
                if (request.asset is SoundManager soundManager)
                {
                    if(TryGetCoreData(out var currentCoreData))
                    {
                        AssignCoreData(soundManager, currentCoreData);
                    }
                    else
                    {
                        StartGenerateUserData(soundManager);
                    }
                }
                else
                {
                    Debug.LogError(Utility.LogTitle + $"Load {nameof(SoundManager)} fail, " +
                        $"please import it and place it in the Resources folder, and go to Tools/Preference, switch to the last tab and hit [Regenerate User Data]");
                }
            }
        }

		private static void StartGenerateUserData(SoundManager soundManager)
		{
			string resourcePath;
			resourcePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(soundManager));

			string coreDataPath = GetAssetSavePath(resourcePath, CoreDataResourcesPath);
            var coreData = CreateCoreData(coreDataPath, out string audioAssetOutputPath);
			AssignCoreData(soundManager, coreData);

			CreateSettingIfNotExist<RuntimeSetting>(GetAssetSavePath(resourcePath, RuntimeSettingPath));
            var editorSetting = CreateSettingIfNotExist<EditorSetting>(GetAssetSavePath(resourcePath, EditorSettingPath));
			editorSetting.AssetOutputPath = audioAssetOutputPath;
			EditorUtility.SetDirty(editorSetting);
            AssetDatabase.SaveAssets();
            _isGenerating = false;
        }

		private static string GetAssetSavePath(string resourcesPath, string relativePath)
		{
			return Path.Combine(resourcesPath, relativePath + ".asset");
		}

		private static BroAudioData CreateCoreData(string coreDataPath, out string audioAssetOutputpath)
		{
			BroAudioData coreData = ScriptableObject.CreateInstance<BroAudioData>();
			GetInitialData(coreData.AddAsset, out audioAssetOutputpath);
			AssetDatabase.CreateAsset(coreData, coreDataPath);
            EditorUtility.SetDirty(coreData);
            return coreData;
		}

		private static void GetInitialData(Action<AudioAsset> onGetAsset, out string audioAssetOutputPath)
		{
			if (TryGetOldCoreDataTextAsset(out var oldTextAsset) && TryParseCoreData(oldTextAsset, out var oldCoreData))
			{
                audioAssetOutputPath = oldCoreData.AssetOutputPath;
				foreach (string guid in oldCoreData.GUIDs)
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var asset = AssetDatabase.LoadAssetAtPath<AudioAsset>(assetPath);
					onGetAsset?.Invoke(asset);
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
			else
			{
				audioAssetOutputPath = DefaultAssetOutputPath;
                var demoAsset = AssetDatabase.LoadAssetAtPath<AudioAsset>(DefaultAssetOutputPath + "/Demo.asset");
				onGetAsset?.Invoke(demoAsset);
			}
		}

		private static void AssignCoreData(SoundManager soundManager, BroAudioData coreData)
		{
			soundManager.AssignCoreData(coreData);
			PrefabUtility.SavePrefabAsset(soundManager.gameObject);
		}

		private static T CreateSettingIfNotExist<T>(string path) where T : ScriptableObject
		{
			T setting = null;
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

		private static bool TryLoadResources<T>(string path, out T resouece) where T : UnityEngine.Object
		{
			resouece = Resources.Load<T>(path);
			return resouece != null;
		}
	}
#endif
}