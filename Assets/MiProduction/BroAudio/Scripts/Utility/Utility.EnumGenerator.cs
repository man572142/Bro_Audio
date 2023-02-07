using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _nameSpace = "MiProduction.BroAudio.Library";

		private static void GenerateEnum(string libraryName, IEnumerable<AudioData> currentAudioDatas)
		{
			string enumsFullPath = GetFullPath(EnumsPath);
			if (!Directory.Exists(enumsFullPath))
			{
				Directory.CreateDirectory(enumsFullPath);
			}
			var datasToWrite = currentAudioDatas.Where(x => x.LibraryName == libraryName);
			WriteEnumFile(enumsFullPath,libraryName, datasToWrite);
			
			AssetDatabase.Refresh();
		}

		private static void WriteEnumFile(string enumsFullPath,string libraryName, IEnumerable<AudioData> datasToWrite)
		{
            using (StreamWriter streamWriter = new StreamWriter(GetFullFilePath(EnumsPath,libraryName + ".cs")))
            {
                streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
                streamWriter.WriteLine("namespace " + _nameSpace + " {");
                streamWriter.WriteLine("public enum " + libraryName);
                streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				foreach(var data in datasToWrite)
				{
					if (IsValidName(libraryName, out ValidationErrorCode errorCode))
					{
						streamWriter.WriteLine($"\t{data.Name} = {data.ID},");
					}
					else
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