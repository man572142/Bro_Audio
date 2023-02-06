using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _defaultRootPath = "MiProduction/BroAudio";
		private const string _defaultLocalEnumsPath = "Scripts/Enums";
		public const string CoreDataFileName = "BroAudioData.json";

		public static readonly string UnityAssetsRootPath = Application.dataPath.Replace("/Assets", string.Empty);

		public static string RootPath { get; set; } = _defaultRootPath;

		private static string _enumsLocalPath = _defaultLocalEnumsPath;

		public static string EnumsPath 
		{
			get => _enumsLocalPath.AppendRootPath();
			set 
			{
				if(!string.IsNullOrEmpty(value))
				{
					Debug.Log("original:" + value);
					_enumsLocalPath = value.Substring(RootPath.Length + 1);
					Debug.Log($"enumsLocalPath:{_enumsLocalPath}");
				}
			}
			
		}


		public static string AppendRootPath(this string localPath) => System.IO.Path.Combine(RootPath, localPath);
		public static string GetFullPath(string path) => System.IO.Path.Combine(UnityAssetsRootPath,path);
		public static string GetFilePath(string path,string fileName) => System.IO.Path.Combine(path,fileName);
		public static string GetFullFilePath(string path,string fileName) => System.IO.Path.Combine(UnityAssetsRootPath, path, fileName);
	}

}