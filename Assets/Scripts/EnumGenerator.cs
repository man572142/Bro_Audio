#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public static class EnumGenerator
{
    const string _folderPath = "Assets/Scripts/Enums/";

    public static void Generate(string enumName,string[] enums)
    {
        string filePathAndName = _folderPath + enumName + ".cs";

        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
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
                    streamWriter.WriteLine("\t" + enums[i] + ",");                
                }
                
            }
            streamWriter.WriteLine("}");
        }
        AssetDatabase.Refresh();
    }
}
#endif