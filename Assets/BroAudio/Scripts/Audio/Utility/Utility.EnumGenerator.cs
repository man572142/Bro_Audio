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
			if (!Directory.Exists(DefaultEnumsPath))
			{
				Directory.CreateDirectory(DefaultEnumsPath);
			}
			var datasToWrite = currentAudioDatas.Where(x => x.LibraryName == libraryName);
			WriteEnumFile(libraryName, datasToWrite);
			
			AssetDatabase.Refresh();
		}

		private static void WriteEnumFile(string libraryName, IEnumerable<AudioData> datasToWrite)
		{
			string filePathAndName = DefaultEnumsPath + "/" + libraryName + ".cs";

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
                streamWriter.WriteLine("namespace " + _nameSpace + " {");
                streamWriter.WriteLine("public enum " + libraryName);
                streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				foreach(var data in datasToWrite)
				{
					if (IsValidEnum(libraryName, data.Name))
					{
						streamWriter.WriteLine($"\t{data.Name} = {data.ID},");
					}
				}
                streamWriter.WriteLine("}}");
            }
        }

		private static bool IsValidEnum(string enumTypeName, string enumName)
		{
			if (String.IsNullOrWhiteSpace(enumName))
			{
				LogError("There is an empty name in " + enumTypeName);
				return false;
			}
			else if (!Regex.IsMatch(enumName, @"^[a-zA-Z]+$"))
			{
				LogError($"{enumName} is not a valid name for library of " + enumTypeName);
				return false;
			}
			return true;
		}
	} 
}