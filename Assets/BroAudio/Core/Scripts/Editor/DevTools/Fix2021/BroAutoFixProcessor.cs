#if UNITY_2021 && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

namespace Ami.BroAudio.Tools
{
    public class BroAutoFixProcessor : AssetModificationProcessor
    {
        public const string PrefKey = "AudioMixerPath";

        static string[] OnWillSaveAssets(string[] paths)
        {
            if(paths !=  null && paths.Length > 0)
            {
                // skip any changes on AudioMixer
                string mixerPath = SessionState.GetString(PrefKey, string.Empty);
                if (!string.IsNullOrEmpty(mixerPath))
                {
                    return paths.Where(x => !x.Equals(mixerPath, System.StringComparison.Ordinal)).ToArray();
                }
            }

            return paths;
        }
    } 
}
#endif
