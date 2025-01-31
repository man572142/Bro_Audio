using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Editor
{
#if UNITY_EDITOR
	public static class BroUserDataGenerator
	{
		private static bool _isLoading = false;

		public static void CheckAndGenerateUserData()
		{
			if (_isLoading)
			{
				return;
			}
			_isLoading = true;

            var request = Resources.LoadAsync<SoundManager>(nameof(SoundManager));
            request.completed += OnGetSoundManager;

            void OnGetSoundManager(AsyncOperation operation)
            {
                request.completed -= OnGetSoundManager;          
                if (request.asset is SoundManager soundManager)
                {
                    if(TryGetCoreData(out var currentCoreData))
                    {
                        soundManager.AssignCoreData(currentCoreData);
                        BroUpdater.Process(soundManager.Mixer, currentCoreData);
                    }
                    else
                    {
                        StartGeneratingUserData(soundManager);
                    }
                }
                else
                {
                    Debug.LogError(Utility.LogTitle + $"Load {nameof(SoundManager)} fail, " +
                        $"please import it and place it in the Resources folder, and go to Tools/Preferences, switch to the last tab and hit [Regenerate User Data]");
                }
                _isLoading = false;
            }
        }

        private static void StartGeneratingUserData(SoundManager soundManager)
		{
			string resourcePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(soundManager));
			string coreDataPath = GetAssetSavePath(resourcePath, CoreDataResourcesPath);
            var coreData = CreateCoreData(coreDataPath, out string audioAssetOutputPath);
            soundManager.AssignCoreData(coreData);

			var runtimeSetting = CreateScriptableObjectIfNotExist<RuntimeSetting>(GetAssetSavePath(resourcePath, RuntimeSettingPath));
            runtimeSetting.GlobalPlaybackGroup = CreateScriptableObjectIfNotExist<DefaultPlaybackGroup>(GetAssetSavePath(resourcePath, GlobalPlaybackGroupPath));
            EditorUtility.SetDirty(runtimeSetting);

            string editorResourcesPath = resourcePath.Replace(ResourcesFolder, $"{EditorFolder}/{ResourcesFolder}");
            var editorSetting = CreateScriptableObjectIfNotExist<EditorSetting>(GetAssetSavePath(editorResourcesPath, EditorSettingPath));
			editorSetting.AssetOutputPath = audioAssetOutputPath;
			EditorUtility.SetDirty(editorSetting);

            AssetDatabase.SaveAssets();
        }

		private static string GetAssetSavePath(string resourcesPath, string relativePath)
		{
			return Combine(resourcesPath, relativePath + ".asset");
		}

		private static BroAudioData CreateCoreData(string coreDataPath, out string audioAssetOutputpath)
		{
			BroAudioData coreData = ScriptableObject.CreateInstance<BroAudioData>();
            coreData.UpdateVersion();
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
                string broPath = DefaultAssetOutputPath.Remove(DefaultAssetOutputPath.LastIndexOf('/'));
                string demoAssetPath = Combine(DefaultAssetOutputPath, "Demo.asset");
                if (Directory.Exists(Combine(broPath, "Demo")))
                {
                    var demoAsset = AssetDatabase.LoadAssetAtPath<AudioAsset>(demoAssetPath);
                    onGetAsset?.Invoke(demoAsset);
                }
                else
                {
                    AssetDatabase.DeleteAsset(demoAssetPath);
                }
			}
		}
	}
#endif
}