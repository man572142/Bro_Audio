#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public static class EnumGenerator
{
    public static void Generate(string enumsPath,string enumName,string[] enums)
    {
        string filePathAndName = enumsPath + "/" + enumName + ".cs";

        if (!Directory.Exists(enumsPath))
        {
            Directory.CreateDirectory(enumsPath);
        }

        if(File.Exists(filePathAndName))
        {
            File.Delete(filePathAndName);
        }

        using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
        {
            streamWriter.WriteLine("public enum " + enumName);
            streamWriter.WriteLine("{");
            streamWriter.WriteLine("\t" + "None" + ",");
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