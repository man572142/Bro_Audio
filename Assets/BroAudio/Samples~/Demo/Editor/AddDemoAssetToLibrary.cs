using System.Linq;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class AddDemoAssetToLibrary : AssetPostprocessor
    {
        private const string TargetFile = "Demo.asset";
        
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Length == 0)
            {
                return;
            }
            
            var path = importedAssets.FirstOrDefault(x => x.EndsWith(TargetFile));
            if (!string.IsNullOrEmpty(path))
            {
                var demoAsset = AssetDatabase.LoadAssetAtPath<AudioAsset>(path);
                if (demoAsset != null && BroEditorUtility.TryGetCoreData(out var coreData))
                {
                    coreData.AddAsset(demoAsset);
                    EditorUtility.SetDirty(coreData);
                    Debug.Log(Utility.LogTitle + "Demo has been added to BroAudioData.asset");
                }
            }
            
#if !BroAudio_DevOnly
            Ami.BroAudio.Tools.TildeFolderImporter.DeleteCallerScript();
#endif
        }
    }

}