#if UNITY_2021 && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Ami.BroAudio.Editor;

namespace Ami.BroAudio.Tools
{
    public class BroAutoFixProcessor : UnityEditor.AssetModificationProcessor
	{
        public const string PrefKey = "AudioMixerPath";

        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths != null && paths.Length > 0)
            {
                string mixerPath = SessionState.GetString(PrefKey, string.Empty);
                if (!string.IsNullOrEmpty(mixerPath) 
                    && BroEditorUtility.EditorSetting != null && !BroEditorUtility.EditorSetting.AcceptAudioMixerModificationIn2021)
                {
					// skip any changes on AudioMixer
					return paths.Where(x => !x.Equals(mixerPath, System.StringComparison.Ordinal)).ToArray();
                }
            }

            return paths;
        }
    } 
}
#endif
