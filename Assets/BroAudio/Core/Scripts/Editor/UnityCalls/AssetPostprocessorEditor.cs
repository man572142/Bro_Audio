using UnityEditor;

namespace Ami.BroAudio.Editor
{
    public class AssetPostprocessorEditor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            OnDeleteAssets(deletedAssets);
            OnReimportAsset(importedAssets);
            if(importedAssets.Length > 0 && importedAssets[0].Contains("BroAudio"))
            {
                BroEditorUtility.FixDuplicateSoundIDs();
                BroUserDataGenerator.CheckAndGenerateUserData();
            }
        }

        private static void OnReimportAsset(string[] importedAssets)
        {
            if (importedAssets.Length > 0 && EditorWindow.HasOpenInstances<ClipEditorWindow>())
            {
                ClipEditorWindow window = EditorWindow.GetWindow<ClipEditorWindow>(null, false);
                window.OnPostprocessAllAssets();
            }
        }

        private static void OnDeleteAssets(string[] deletedAssets)
        {
            if (deletedAssets == null || deletedAssets.Length == 0)
            {
                return;
            }

            BroEditorUtility.RemoveEmptyDatas();

            if (EditorWindow.HasOpenInstances<LibraryManagerWindow>())
            {
                var editorWindow = EditorWindow.GetWindow<LibraryManagerWindow>(null, false);

                foreach (string path in deletedAssets)
                {
                    if(path.Contains(".asset"))
                    {
                        editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(path));
                    }
                }
            }
        }
    }
}