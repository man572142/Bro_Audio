using System.Linq;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor;
using UnityEditor;

namespace Ami.BroAudio.Demo
{
    public class AddDemoAssetToLibrary : AssetPostprocessor
    {
        private const string SearchFilter = "Demo t:AudioAsset";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Length == 0)
            {
                return;
            }
            
            if (BroEditorUtility.TryGetCoreData(out var coreData))
            {
                var guid = AssetDatabase.FindAssets(SearchFilter).FirstOrDefault();
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var demoAsset = AssetDatabase.LoadAssetAtPath<AudioAsset>(path);
                if (demoAsset != null)
                {
                    coreData.AddAsset(demoAsset);
                    EditorUtility.SetDirty(coreData);
                    
#if BroAudio_DevOnly
                    UnityEngine.Debug.Log(Utility.LogTitle + "Demo has been added to BroAudioData.asset");
#else
                    Tools.TildeFolderImporter.DeleteCallerScript();
#endif
                }
            }
        }
    }

}