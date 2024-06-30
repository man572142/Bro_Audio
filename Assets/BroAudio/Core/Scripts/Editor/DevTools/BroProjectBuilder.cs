using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Ami.BroAudio.Editor.DevTool
{
    public static class BroProjectBuilder
    {
        public static void Build()
        {
            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }

            string[] args = Environment.GetCommandLineArgs();
            string outputPath = GetArgumentValue(args, "-param1");
            string buildTargetArg = GetArgumentValue(args, "-buildTarget");

            if (string.IsNullOrEmpty(outputPath) || !Enum.TryParse(buildTargetArg, out BuildTarget buildTarget))
            {
                UnityEngine.Debug.LogError($"Invalid arguments path:{outputPath}, buildTarget:{buildTargetArg}");
                throw new Exception();
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            options.target = buildTarget;
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
}