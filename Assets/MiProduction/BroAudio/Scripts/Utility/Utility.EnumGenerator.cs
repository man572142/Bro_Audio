using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using MiProduction.BroAudio.Asset.Core;
using MiProduction.BroAudio.Asset;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _nameSpace = "MiProduction.BroAudio.Library";

		public static void GenerateEnum(IAudioAsset asset)
		{
			string enumsFullPath = GetFullPath(EnumsPath);
			if (!Directory.Exists(enumsFullPath))
			{
				Directory.CreateDirectory(enumsFullPath);
			}
			
			string fullFilePath = GetFullFilePath(EnumsPath,asset.AssetName + ".cs");

			IEnumerable<AudioData> datasToWrite = asset.AllAudioData;
			WriteEnumTextFile(asset.AssetName, fullFilePath, datasToWrite);

			AssetDatabase.Refresh();
		}

		private static void DeleteEnumFile(string libraryName)
		{
			string fullFilePath = GetFullFilePath(EnumsPath, libraryName + ".cs");
			File.Delete(fullFilePath);
			File.Delete(fullFilePath + ".meta");
		}

		private static void WriteEnumTextFile(string libraryName,string fullFilePath, IEnumerable<AudioData> datasToWrite)
		{
			using (StreamWriter streamWriter = new StreamWriter(fullFilePath))
			{
				streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
				streamWriter.WriteLine("namespace " + _nameSpace + " {");
				streamWriter.WriteLine("public enum " + libraryName);
				streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				foreach (var data in datasToWrite)
				{
					if (IsInvalidName(libraryName, out ValidationErrorCode errorCode))
					{
						switch (errorCode)
						{
							case ValidationErrorCode.NoError:
								break;
							case ValidationErrorCode.IsNullOrEmpty:
								LogError($"There is an Empty name in {libraryName}.");
								break;
							case ValidationErrorCode.StartWithNumber:
								LogError($"Name should not start with a number! {data.Name} from {libraryName} library is invalid");
								break;
							case ValidationErrorCode.ContainsInvalidWord:
								LogError($"Name can only use \"Letter\",\"Number\" and \"_(Undersocre)\",{ data.Name} from {libraryName} library is invalid");
								break;
						}
						
					}
					else
					{
						streamWriter.WriteLine($"\t{data.Name} = {data.ID},");
					}
				}
				streamWriter.WriteLine("}}");
			}
		}

		public static Type GetEnumType(string enumName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = assembly.GetType(enumName);
				if (type == null)
					continue;
				if (type.IsEnum)
					return type;
			}
			return null;
		}

		public static bool HasEnumType(string enumName)
		{
			return GetEnumType(enumName) != null;
		}
	}
}