#if UNITY_2021 && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.Extension.Reflection;
using static Ami.BroAudio.Tools.BroAutoFixProcessor;

namespace Ami.BroAudio.Tools
{
    public class BroAutoFixTrigger
    {
        [InitializeOnEnterPlayMode]
        static void OnFix()
        {
            string mixerPath = SessionState.GetString(PrefKey, string.Empty);
            if (string.IsNullOrEmpty(mixerPath))
            {
                GameObject managerObj = Resources.Load(nameof(SoundManager)) as GameObject;
                SoundManager manager = managerObj.GetComponent<SoundManager>();
                if (manager.Mixer)
                {
                    BroAudioReflection.FixAudioReverbZoneIssue(manager.Mixer);
                    SessionState.SetString(PrefKey, AssetDatabase.GetAssetPath(manager.Mixer));
                }
            }
        }
    }
}
#endif