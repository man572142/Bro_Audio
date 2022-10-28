using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EnumGenerator
{
    private List<string> _enumList;


    public void Generate(string enumsPath,string enumName,string[] enumsToWrite)
    {
        if (!Directory.Exists(enumsPath))
        {
            Directory.CreateDirectory(enumsPath);
        }

        WriteCoreLibraryEnum(enumsPath + "/CoreLibraryEnum.cs",enumName,enumsToWrite);

        string filePathAndName = enumsPath + "/" + enumName + ".cs";
        if (File.Exists(filePathAndName))
        {
            string[] currentEnumNames = Enum.GetNames(Type.GetType(enumName));
            enumsToWrite = currentEnumNames.Concat(enumsToWrite).Distinct().ToArray();
        }

        using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
        {
            streamWriter.WriteLine("// Auto-Generate script,DO NOT EDIT!");
            streamWriter.WriteLine("public enum " + enumName);
            streamWriter.WriteLine("{");

            for (int i = 0; i < enumsToWrite.Length; i++)
            {
                string newEnum = enumsToWrite[i].Replace(" ", string.Empty);
                int index = _enumList.IndexOf(newEnum);
                streamWriter.WriteLine($"\t{newEnum} = {index},");
            }
            streamWriter.WriteLine("}");
        }
        AssetDatabase.Refresh();
    }

	private void WriteCoreLibraryEnum(string filePath,string enumName,string[] enumsToWrite)
	{
        _enumList = new List<string>();
        _enumList.AddRange(Enum.GetNames(typeof(CoreLibraryEnum)));
        foreach(string enumString in enumsToWrite)
		{
            if(String.IsNullOrWhiteSpace(enumString))
			{
                UnityEngine.Debug.LogWarning("[SoundSystem] there is an empty name in " + enumName);
            }
            else if (!Regex.IsMatch(enumString, @"^[a-zA-Z]+$"))
			{
                UnityEngine.Debug.LogWarning($"[SoundSystem] {enumString} is not a valid name for library of " + enumName);
            }
            else if(!_enumList.Contains(enumString) && enumString != "None")
			{
                _enumList.Add(enumString.Replace(" ",string.Empty));
			}
		}
        
        using (StreamWriter streamWriter = new StreamWriter(filePath))
        {
            streamWriter.WriteLine("// DON'T EDIT OR DELETE THIS ENUM!");
            streamWriter.WriteLine("// this is the actual enums that BroAudio system use under the hood , all other user-define enums will be added to this after enum generation.");
            streamWriter.WriteLine("public enum CoreLibraryEnum");
            streamWriter.WriteLine("{");
            for (int i = 0; i < _enumList.Count; i++)
            {
                streamWriter.WriteLine($"\t{_enumList[i]} = {i},");
            }
            streamWriter.WriteLine("}");
        }
        AssetDatabase.Refresh();
    }
}