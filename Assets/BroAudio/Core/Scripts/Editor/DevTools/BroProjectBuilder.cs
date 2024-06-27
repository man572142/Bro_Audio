using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public static class BroProjectBuilder
{
    public static void BuildBroAudio()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        string[] args = System.Environment.GetCommandLineArgs();
        string outputPath = GetArgumentValue(args, "-param1");

        if(string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        UnityEngine.Debug.Log(outputPath);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.target = BuildTarget.StandaloneWindows;
        options.scenes = scenes.ToArray();
        options.locationPathName = outputPath;
        BuildPipeline.BuildPlayer(options);
    }

    private static string GetArgumentValue(string[] args, string argumentName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == argumentName && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
