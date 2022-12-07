using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using static MiProduction.BroAudio.BroAudioUtility;

namespace MiProduction.BroAudio
{
	public static class EnumGenerator
	{
		private const string _nameSpace = "MiProduction.BroAudio.";
		//private const string _coreEnumsFileName = "/CoreLibraryEnum.cs";

		public static void Generate(string enumsPath, string enumName, AudioData[] data)
		{
			if (!Directory.Exists(enumsPath))
			{
				Directory.CreateDirectory(enumsPath);
			}

			WriteEnum(enumsPath, enumName, data);
			
			AssetDatabase.Refresh();
		}

		private static void WriteEnum(string enumsPath, string enumName, AudioData[] datas)
		{
			string filePathAndName = enumsPath + "/" + enumName + ".cs";

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
                streamWriter.WriteLine("namespace MiProduction.BroAudio {");
                streamWriter.WriteLine("public enum " + enumName);
                streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				for (int i = 0; i < datas.Length; i++)
                {
                    streamWriter.WriteLine($"\t{datas[i].Name} = {datas[i].ID},");
                }
                streamWriter.WriteLine("}}");
            }
        }

		private static bool IsValidEnum(string enumName, string enumString)
		{
			if (String.IsNullOrWhiteSpace(enumString))
			{
				UnityEngine.Debug.LogError("[SoundSystem] there is an empty name in " + enumName);
				return false;
			}
			else if (!Regex.IsMatch(enumString, @"^[a-zA-Z]+$"))
			{
				UnityEngine.Debug.LogError($"[SoundSystem] {enumString} is not a valid name for library of " + enumName);
				return false;
			}
			return true;
		}
	} 
}