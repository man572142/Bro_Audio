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

        [MenuItem(MenuItem_BroAudio + "Others/Regenerate User Data")]
        private static void RegenerateUserData() => CheckAndGenerateUserData();

        public static void CheckAndGenerateUserData(Action onFinished = null)
        {
            if (_isLoading)
            {
                return;
            }
            _isLoading = true;

            ResourceRequest request;

            try
            {
                EnsureAllResources();
                EnsureDefaultAssetOutputPath();

                request = Resources.LoadAsync<SoundManager>(nameof(SoundManager));
                request.completed += OnGetSoundManager;
            }
            catch
            {
                _isLoading = false;
                throw;
            }

            void OnGetSoundManager(AsyncOperation operation)
            {
                try
                {
                    request.completed -= OnGetSoundManager;
                    if (request.asset is SoundManager soundManager)
                    {
                        if (TryGetCoreData(out var currentCoreData))
                        {
                            soundManager.AssignCoreData(currentCoreData);
                            BroUpdater.Process(soundManager.AudioMixer, currentCoreData);
                        }
                        else
                        {
                            StartGeneratingUserData(soundManager);
                        }
                        onFinished?.Invoke();
                    }
                    else
                    {
                        Debug.LogError(Utility.LogTitle + $"Load {nameof(SoundManager)} fail, " +
                            $"please import it and place it in the Resources folder, and go to Tools/Preferences, switch to the last tab and hit [Regenerate User Data]");
                    }
                }
                finally
                {
                    _isLoading = false;
                }
            }
        }

        private static void EnsureAllResources([System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            // Loop upwards and find all Resources~ directories
            string currentPath = Path.GetDirectoryName(callerFilePath);
            var cwd = Directory.GetCurrentDirectory();
            int changed = 0;

            // Loop until we reach the project root
            while (!string.IsNullOrEmpty(currentPath) && (currentPath.Contains(cwd) || !Path.IsPathRooted(currentPath)))
            {
                var resourcesPath = Path.Combine(currentPath, "Resources~");

                if (Directory.Exists(resourcesPath))
                {
                    changed += CopyResourcesToLocalIfNotFound(resourcesPath);
                }

                currentPath = Path.GetDirectoryName(currentPath);
            }

            if (changed > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            static int CopyResourcesToLocalIfNotFound(string resourcesPath)
            {
                int changed = 0;
                string targetBasePath = Path.Combine(Application.dataPath, "BroAudio");

                // Copy files from Resources~/
                changed += CopyDirectoryIfNotExists(resourcesPath, Path.Combine(targetBasePath, "Resources"));

                // Copy files from Resources~/Editor/
                string editorResourcesPath = Path.Combine(resourcesPath, "Editor");
                if (Directory.Exists(editorResourcesPath))
                {
                    changed += CopyDirectoryIfNotExists(editorResourcesPath, Path.Combine(targetBasePath, "Editor", "Resources"));
                }

                return changed;
            }

            static int CopyDirectoryIfNotExists(string sourceDir, string targetDir)
            {
                int changed = 0;
                var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly);

                foreach (string sourceFile in files)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(targetDir, fileName);
                    string sourceMetaFile = sourceFile + ".meta";
                    string targetMetaFile = targetFile + ".meta";

                    // Only copy if no resource with the same name exists in Resources folders
                    if (!ResourceExistsInResourcesFolders(fileName))
                    {
                        if (!Directory.Exists(targetDir))
                        {
                            Directory.CreateDirectory(targetDir);
                        }

                        File.Copy(sourceFile, targetFile, true);
#if BroAudio_DevOnly
                        Debug.Log($"Copied {fileName} to {targetDir}");
#endif
                        changed++;

                        // Copy .meta file if it exists
                        if (File.Exists(sourceMetaFile))
                        {
                            File.Copy(sourceMetaFile, targetMetaFile, true);
#if BroAudio_DevOnly
                            Debug.Log($"Copied {fileName}.meta to {targetDir}");
#endif
                            changed++;
                        }
                    }
                }

                return changed;
            }

            static bool ResourceExistsInResourcesFolders(string fileName)
            {
                // Remove .asset extension if present for asset name matching
                string assetName = Path.GetFileNameWithoutExtension(fileName);

                // Search for existing assets with the same name in all Resources folders
                string[] guids = AssetDatabase.FindAssets(assetName, new[] { "Assets" });

                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string assetFileName = Path.GetFileName(assetPath);

                    // Check if the asset is in a Resources folder and has the same name
                    if (assetPath.Contains("/Resources/") && assetFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Not found, so do a deeper dive to see if it exists in any subfolders
                foreach (string assetPath in Directory.EnumerateFiles("Assets", fileName, SearchOption.AllDirectories))
                {
                    if (assetPath.Contains("/Resources/"))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        
        private static void EnsureDefaultAssetOutputPath()
        {
            // Determine target output path from EditorSetting if available, otherwise use default
            string targetPath = DefaultAssetOutputPath;
            if (TryLoadResources<EditorSetting>(EditorSettingPath, out var setting))
            {
                if (!string.IsNullOrWhiteSpace(setting.AssetOutputPath) && targetPath.StartsWith("Assets"))
                {
                    targetPath = setting.AssetOutputPath;
                }
                else
                {
                    // Ensure a value is written to the setting so future reads are consistent
                    setting.AssetOutputPath = DefaultAssetOutputPath;
                    EditorUtility.SetDirty(setting);
                    AssetDatabase.SaveAssetIfDirty(setting);
                }
            }

            // If folder already exists, nothing to do
            if (AssetDatabase.IsValidFolder(targetPath))
            {
                return;
            }

            // Create folders recursively using AssetDatabase.CreateFolder
            string[] segments = targetPath.Split('/');
            if (segments.Length == 0)
            {
                return;
            }

            string parent = segments[0]; // should be "Assets"
            for (int i = 1; i < segments.Length; i++)
            {
                string current = parent + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(current))
                {
                    AssetDatabase.CreateFolder(parent, segments[i]);
                }
                parent = current;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void StartGeneratingUserData(SoundManager soundManager)
        {
            string resourcePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(soundManager));
            string coreDataPath = GetAssetSavePath(resourcePath, BroEditorUtility.CoreDataResourcesPath);
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
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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
                string demoAssetPath = Combine(DefaultAssetOutputPath, Demo + ".asset");
                if (Directory.Exists(Combine(broPath, Demo)))
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