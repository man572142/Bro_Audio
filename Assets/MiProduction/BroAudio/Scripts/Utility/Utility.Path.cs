using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _defaultRootPath = "Assets/MiProduction/BroAudio";
		private const string _defaultLocalEnumsPath = "Scripts/Enums";
		private const string _defaultLocalLibraryPath = "Library";
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

		private static string _enumsLocalPath = _defaultLocalEnumsPath;

		public static string EnumsPath 
		{
			get => _enumsLocalPath.WithRootPath();
			set 
			{
				if(!string.IsNullOrEmpty(value))
				{
					_enumsLocalPath = value.Substring(RootPath.Length + 1);
				}
			}
		}

		private static string _libraryLocalPath = _defaultLocalLibraryPath;
		public static string LibraryPath
		{
			get => _libraryLocalPath.WithRootPath();
			set
			{
				if(!string.IsNullOrEmpty(value))
				{
					_libraryLocalPath = value.Substring(RootPath.Length + 1);
				}
			}
		}

		public static string WithRootPath(this string localPath) => Combine(RootPath, localPath);
		public static string GetFullPath(string path) => Combine(UnityAssetsRootPath,path);
		public static string GetFilePath(string path,string fileName) => Combine(path,fileName);
		public static string GetFullFilePath(string path,string fileName) => Combine(UnityAssetsRootPath, path, fileName);

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