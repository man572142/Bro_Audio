#if UNITY_2021 && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.Extension.Reflection;
using static Ami.BroAudio.Tools.BroAutoFixProcessor;
using Ami.BroAudio.Editor;

namespace Ami.BroAudio.Tools
{
    public class BroAutoFixTrigger : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if(BroEditorUtility.EditorSetting.ManualFix)
            {
                return;
            }

            string mixerPath = SessionState.GetString(PrefKey, string.Empty);
            if (string.IsNullOrEmpty(mixerPath))
            {
                GameObject managerObj = Resources.Load(nameof(SoundManager)) as GameObject;
                if (managerObj == null)
                {
                    AssetDatabase.importPackageCompleted += OnPackageImportCompleted;
                    return;
                }

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

        private static void OnPackageImportCompleted(string packageName)
        {
            GameObject managerObj = Resources.Load(nameof(SoundManager)) as GameObject;

            if(managerObj != null)
            {
                AssetDatabase.importPackageCompleted -= OnPackageImportCompleted;
                FixAudioReverbZoneIssue(managerObj);
            }
        }
    }
}
#endif