using Ami.BroAudio.Editor.Setting;
using UnityEditor;
using UnityEngine;

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
            // Migrate legacy Core/Scripts layout before any data generation.
            FileStructureUpgrader.TryUpgradeFileStructure();
            
            var editorSetting = Resources.Load<EditorSetting>(BroEditorUtility.EditorSettingPath);
            if (!editorSetting || editorSetting.HasSetupWizardAutoLaunched)
            {
                return;
            }
            
            SetupWizardWindow.ShowWindow();
            editorSetting.HasSetupWizardAutoLaunched = true;
            EditorUtility.SetDirty(editorSetting);
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