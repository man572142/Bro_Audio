#if UNITY_2021 && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.Extension.Reflection;
using Ami.BroAudio.Editor;

namespace Ami.BroAudio.Tools
{
    public class BroAutoFixTrigger : AssetPostprocessor
    {
        public static string PrefKey => BroAutoFixProcessor.PrefKey;
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            // When importing a package for the first time, Resources.Load() will return null.
            // To avoid the warning raised when accessing BroEditorUtility.EditorSetting, we manually load it.
            var setting = Resources.Load<EditorSetting>(BroName.EditorSettingPath);
            if (setting == null)
            {
                AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
                return;
            }

            FixIfNeeded();
        }

        private static void OnPackageImportCompleted(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnPackageImportCompleted;
            FixIfNeeded();
        }

        private static void FixIfNeeded()
        {
            if (BroEditorUtility.EditorSetting.ManualFix)
            {
                return;
            }

            string mixerPath = SessionState.GetString(PrefKey, string.Empty);
            if(string.IsNullOrEmpty(mixerPath))
            {
                GameObject managerObj = Resources.Load(nameof(SoundManager)) as GameObject;
                FixAudioReverbZoneIssue(managerObj);
            }
        }

        public static void FixAudioReverbZoneIssue(GameObject managerObj)
        {
            SoundManager manager = managerObj.GetComponent<SoundManager>();
            if (manager.Mixer)
            {
                BroAudioReflection.FixAudioReverbZoneIssue(manager.Mixer);
                SessionState.SetString(PrefKey, AssetDatabase.GetAssetPath(manager.Mixer));
            }
        }
    }
}
#endif