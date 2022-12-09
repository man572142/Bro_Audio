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
		private const string _nameSpace = "MiProduction.BroAudio.";

		public static void GenerateEnum(string enumsPath, string enumTypeName, AudioData[] datas)
		{
			if (!Directory.Exists(enumsPath))
			{
				Directory.CreateDirectory(enumsPath);
			}

			WriteEnumFile(enumsPath, enumTypeName, datas);
			
			AssetDatabase.Refresh();
		}

		private static void WriteEnumFile(string enumsPath, string enumTypeName, AudioData[] datas)
		{
			string filePathAndName = enumsPath + "/" + enumTypeName + ".cs";

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
                streamWriter.WriteLine("namespace MiProduction.BroAudio {");
                streamWriter.WriteLine("public enum " + enumTypeName);
                streamWriter.WriteLine("{");
				streamWriter.WriteLine("\tNone = 0,");

				for (int i = 0; i < datas.Length; i++)
                {
					if(IsValidEnum(enumTypeName, datas[i].Name))
					{
						streamWriter.WriteLine($"\t{datas[i].Name} = {datas[i].ID},");
					}
                }
                streamWriter.WriteLine("}}");
            }
        }

		private static bool IsValidEnum(string enumTypeName, string enumName)
		{
			if (String.IsNullOrWhiteSpace(enumName))
			{
				UnityEngine.Debug.LogError("[SoundSystem] there is an empty name in " + enumTypeName);
				return false;
			}
			else if (!Regex.IsMatch(enumName, @"^[a-zA-Z]+$"))
			{
				UnityEngine.Debug.LogError($"[SoundSystem] {enumName} is not a valid name for library of " + enumTypeName);
				return false;
			}
			return true;
		}
	} 
}