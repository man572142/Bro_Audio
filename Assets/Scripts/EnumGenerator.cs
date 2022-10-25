#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System;
using System.Linq;

public static class EnumGenerator
{
    public static void Generate(string enumsPath,string enumName,string[] enums,bool replaceOld = false)
    {
        string filePathAndName = enumsPath + "/" + enumName + ".cs";

        if (!Directory.Exists(enumsPath))
        {
            Directory.CreateDirectory(enumsPath);
        }

        bool isFileExists = File.Exists(filePathAndName);
        if (isFileExists)
        {
            if(replaceOld)
			{
                File.Delete(filePathAndName);
            }
			else
			{
                
                string[] currentEnumNames = Enum.GetNames(Type.GetType(enumName));
                enums = currentEnumNames.Concat(enums).Distinct().ToArray();
			}
        }


        using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
        {
            streamWriter.WriteLine("public enum " + enumName);
            streamWriter.WriteLine("{");
            if(!isFileExists)
			{
                streamWriter.WriteLine("\t" + "None,");
            }
            
            for (int i = 0; i < enums.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(enums[i]))
                {
                    UnityEngine.Debug.LogWarning("[SoundSystem] there is an empty name in " + enumName);
                }
                else
                {
                    streamWriter.WriteLine("\t" + enums[i].Replace(" ","") + ",");                
                }
                
            }
            streamWriter.WriteLine("}");
        }
        AssetDatabase.Refresh();
    }
}
#endif