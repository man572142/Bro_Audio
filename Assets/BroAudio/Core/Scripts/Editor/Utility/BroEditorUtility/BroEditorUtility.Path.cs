using UnityEngine;
using System.IO;
using UnityEditor;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public const string CoreDataResourcesPath = BroName.CoreDataName;
        public const string DefaultRelativeAssetOutputPath = "BroAudio/AudioAssets";
        public const string DefaultAssetOutputPath = "Assets/" + DefaultRelativeAssetOutputPath;
        public const string EditorSettingPath = BroName.EditorSettingName;
        public const string RuntimeSettingPath = BroName.RuntimeSettingName;
        public const string GlobalPlaybackGroupPath = BroName.GlobalPlaybackGroupName;

        public const string MainLogoPath = "Logo_Editor";
        public const string TransparentLogoPath = "Logo_Transparent";
        public const string EditorAudioMixerPath = BroName.EditorAudioMixerName;

        public static readonly string UnityProjectRootPath = Application.dataPath.Replace("/Assets", string.Empty);

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
                Debug.LogError(Utility.LogTitle + "The path must be under Assets folder or its sub-folder");
                return false;
            }
            return true;
        }

        public static string ToUnitySeparator(this string value) => value.Replace('\\','/');
        public static string ToMicrosoftSeparator(this string value) => value.Replace('/', '\\');

        #region Path Combine
        public static string Combine(string path1,string path2)
        {
            return path1 + "/" + path2;
        }

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