using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _nameSpace = "MiProduction.BroAudio.";

		private static void GenerateEnum(AudioType audioType, IEnumerable<AudioData> currentAudioDatas)
		{
			if (!Directory.Exists(DefaultEnumsPath))
			{
				Directory.CreateDirectory(DefaultEnumsPath);
			}
			var datasToWrite = currentAudioDatas.Where(x => x.AudioType == audioType);
			WriteEnumFile(audioType, datasToWrite);
			
			AssetDatabase.Refresh();
		}

		private static void WriteEnumFile(AudioType audioType, IEnumerable<AudioData> datasToWrite)
		{
			string enumTypeName = audioType.ToString();
			string filePathAndName = DefaultEnumsPath + "/" + enumTypeName + ".cs";

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
                streamWriter.WriteLine("namespace MiProduction.BroAudio {");
                streamWriter.WriteLine("public enum " + enumTypeName);
                streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				foreach(var data in datasToWrite)
				{
					if (IsValidEnum(enumTypeName, data.Name))
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