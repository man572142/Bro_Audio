using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _defaultRootPath = "Assets/MiProduction/BroAudio";
		private const string _defaultLocalAssetPath = "AudioAssets";
		public const string CoreDataFileName = "BroAudioData.json";

		public static readonly string UnityAssetsRootPath = Application.dataPath.Replace("/Assets", string.Empty);

		private static string _rootPath = _defaultRootPath;
		public static string RootPath 
		{
			get => _rootPath;
			set
			{
				if(!string.IsNullOrEmpty(_rootPath))
				{
					_rootPath = value.ToUnitySeparator();
				}
			}
		}

		private static string _AssetLocalPath = _defaultLocalAssetPath;
		public static string AssetPath
		{
			get => _AssetLocalPath.WithRootPath().EnsureDirectoryExists();
			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					_AssetLocalPath = value.Substring(RootPath.Length + 1);
				}
			}
		}

		public static string WithRootPath(this string localPath) => Combine(RootPath, localPath);
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