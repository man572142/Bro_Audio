using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static Ami.BroAudio.BroLog;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		private const string _defaultAssetOutputPath = "Assets/Ami/BroAudio/AudioAssets";
		public const string CoreDataResourcesPath = "Editor/BroAudioData";

		public static readonly string UnityAssetsRootPath = Application.dataPath.Replace("/Assets", string.Empty);


		private static string _assetOutputPath = string.Empty;
		public static string AssetOutputPath
		{
			get
			{
				if(string.IsNullOrEmpty(_assetOutputPath) && TryGetCoreData(out SerializedCoreData coreData))
				{
					if(string.IsNullOrWhiteSpace(coreData.AssetOutputPath))
					{
						_assetOutputPath = _defaultAssetOutputPath;
					}
					else
					{
						_assetOutputPath = coreData.AssetOutputPath;
					}
				}
				return _assetOutputPath;
			}
			set => _assetOutputPath = value;
		}

		public static string GetFullPath(string path) => Combine(UnityAssetsRootPath,path);
		public static string GetFilePath(string path,string fileName) => Combine(path,fileName);
		public static string GetFullFilePath(string path,string fileName) => Combine(UnityAssetsRootPath, path, fileName);

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
			if (!path.Contains(UnityAssetsRootPath))
			{
				LogError("The path must be under Assets folder or its sub-folder");
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