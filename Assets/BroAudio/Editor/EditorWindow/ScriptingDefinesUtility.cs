using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Editor
{
    public static class ScriptingDefinesUtility
    {
        private const string ManualInitScriptingDefineSymbol = "BroAudio_InitManually";
        
        public static void AddManualInitScriptingDefineSymbol()
        {
            ModifyScriptingDefineSymbols(defines =>
            {
                if (!defines.Contains(ManualInitScriptingDefineSymbol))
                {
                    if (!string.IsNullOrEmpty(defines))
                    {
                        defines += $";{ManualInitScriptingDefineSymbol}";
                    }
                    else
                    {
                        defines = ManualInitScriptingDefineSymbol;
                    }
                    return defines;
                }
                return null;
            });
        }

        public static void RemoveManualInitScriptingDefineSymbol()
        {
            ModifyScriptingDefineSymbols(defines =>
            {
                if (defines.Contains(ManualInitScriptingDefineSymbol))
                {
                    var definesList = defines.Split(';').ToList();
                    definesList.Remove(ManualInitScriptingDefineSymbol);
                    return string.Join(";", definesList);
                }
                return null;
            });
        }

        private static void ModifyScriptingDefineSymbols(System.Func<string, string> modifyAction)
        {
#if UNITY_2022_3_OR_NEWER
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
#else
            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
#endif

            string modifiedDefines = modifyAction(defines);
            if (modifiedDefines != null)
            {
                CompilationPipeline.compilationFinished += OnCompilationFinished;
#if UNITY_2022_3_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(target, modifiedDefines);
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(target, modifiedDefines);
#endif
            }
        }

        private static void OnCompilationFinished(object obj)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
#if BroAudio_InitManually
            Debug.Log($"{LogTitle}Removed {ManualInitScriptingDefineSymbol} scripting define symbol from project settings.");
#else
            Debug.Log($"{LogTitle}Added {ManualInitScriptingDefineSymbol} scripting define symbol to project settings.");
#endif
        }
    }
}