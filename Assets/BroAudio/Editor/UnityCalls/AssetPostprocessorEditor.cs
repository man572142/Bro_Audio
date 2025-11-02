using UnityEditor;

namespace Ami.BroAudio.Editor
{
    public class AssetPostprocessorEditor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            OnReimportAsset(importedAssets);

            if(importedAssets.Length > 0)
            {
                if (importedAssets[0].Contains("BroAudio") ||
                    importedAssets[0].Contains("Bro_Audio") ||
                    importedAssets[0].Contains("com.ami.broaudio"))
                {
                    BroUserDataGenerator.CheckAndGenerateUserData(OnUserDataChecked);
                }
            }
        }

        private static void OnUserDataChecked()
        {
            Setting.SetupWizardWindow.CheckAndShowForFirstSetup();
        }

        private static void OnReimportAsset(string[] importedAssets)
        {
            if (importedAssets.Length > 0 && EditorWindow.HasOpenInstances<ClipEditorWindow>())
            {
                ClipEditorWindow window = EditorWindow.GetWindow<ClipEditorWindow>(null, false);
                window.OnPostprocessAllAssets();
            }
        }
    }
}