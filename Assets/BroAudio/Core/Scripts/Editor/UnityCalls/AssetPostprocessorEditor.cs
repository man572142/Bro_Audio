using UnityEditor;
using System.Collections.Generic;
using static Ami.Extension.EditorVersionAdapter;

namespace Ami.BroAudio.Editor
{
    public class AssetPostprocessorEditor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            OnDeleteAssets(deletedAssets);
            OnReimportAsset(importedAssets);
            if(importedAssets.Length > 0)
            {
                BroUserDataGenerator.CheckAndGenerateUserData();
            }
        }

        private static void OnReimportAsset(string[] importedAssets)
        {
            if (importedAssets.Length > 0 && HasOpenEditorWindow<ClipEditorWindow>())
            {
                ClipEditorWindow window = EditorWindow.GetWindow(typeof(ClipEditorWindow)) as ClipEditorWindow;
                window.OnPostprocessAllAssets();
            }
        }

        private static void OnDeleteAssets(string[] deletedAssets)
        {
            if (deletedAssets == null || deletedAssets.Length == 0)
            {
                return;
            }

            List<string> paths = null;
            foreach (string path in deletedAssets)
            {
                if (path.Contains(BroEditorUtility.DefaultRelativeAssetOutputPath))
                {
                    paths = paths ?? new List<string>();
                    paths.Add(path);
                }
            }

            if (paths != null && paths.Count > 0)
            {
                BroEditorUtility.DeleteAssetRelativeData(deletedAssets);

                if (HasOpenEditorWindow<LibraryManagerWindow>())
                {
                    LibraryManagerWindow editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;

                    foreach (string path in deletedAssets)
                    {
                        editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(path));
                    }
                }
            }
        }
    }

}