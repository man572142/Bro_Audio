#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class EnumGenerator
{
    const string _folderPath = "Assets/Scripts/Enums/";

    [MenuItem("Tools/GenerateEnum")]
    public static void Generate(string enumName,string[] enums)
    {
        //string[] enumEntries = { "Foo", "Goo", "Hoo" };
        string filePathAndName = _folderPath + enumName + ".cs";
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
        {
            streamWriter.WriteLine("public enum " + enumName);
            streamWriter.WriteLine("{");
            for (int i = 0; i < enums.Length; i++)
            {
                streamWriter.WriteLine("\t" + enums[i] + ",");
            }
            streamWriter.WriteLine("}");
        }
        AssetDatabase.Refresh();
    }
}
#endif