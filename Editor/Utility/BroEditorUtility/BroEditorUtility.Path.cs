using UnityEngine;
using System.IO;
using UnityEditor;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public const string CoreDataResourcesPath = Tools.BroName.CoreDataName;
        public const string DefaultRelativeAssetOutputPath = "BroAudio/AudioAssets";
        public const string DefaultAssetOutputPath = "Assets/" + DefaultRelativeAssetOutputPath;
        public static readonly string UnityProjectRootPath = Application.dataPath.Replace("/Assets", string.Empty);
        public const string EditorSettingPath = Tools.BroName.EditorSettingName;
        public const string RuntimeSettingPath = Tools.BroName.RuntimeSettingName;
        public const string GlobalPlaybackGroupPath = Tools.BroName.GlobalPlaybackGroupName;

        public const string MainLogoPath = "Logo_Editor";
        public const string TransparentLogoPath = "Logo_Transparent";
        public const string EditorAudioMixerPath = Tools.BroName.EditorAudioMixerName;

        public static string AssetOutputPath
        {
            get
            {
                if(EditorSetting)
                {
                    if(string.IsNullOrWhiteSpace(EditorSetting.AssetOutputPath))
                    {
                        EditorSetting.AssetOutputPath = DefaultAssetOutputPath;
                        EditorUtility.SetDirty(EditorSetting);
                        AssetDatabase.SaveAssetIfDirty(EditorSetting);
                    }
                    return EditorSetting.AssetOutputPath;
                }
                return DefaultAssetOutputPath;
            }
        }

        public static string GetFullPath(string path) => Combine(UnityProjectRootPath,path);
        public static string GetFilePath(string path,string fileName) => Combine(path,fileName);
        public static string GetFullFilePath(string path,string fileName) => Combine(UnityProjectRootPath, path, fileName);

        public static string EnsureDirectoryExists(this string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static bool IsInProjectFolder(string path)
        {
            if (!path.Contains(Application.dataPath))
            {
                Debug.LogError(Utility.LogTitle + "The path must be under the Assets folder or its subfolders");
                return false;
            }
            return true;
        }

        #region Path Combine
        public static string Combine(string path1,string path2,string path3)
        {
            return path1 + "/" + path2 + "/" + path3;
        }

        public static string Combine(params string[] paths)
        {
            string result = string.Empty;
            for(int i = 0; i < paths.Length; i++)
            {
                if(i == 0)
                {
                    result += paths[i];
                }
                else
                {
                    result += "/" + paths[i];
                }
            }
            return result;
        }
        #endregion
    }
}