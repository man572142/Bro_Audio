#if UNITY_EDITOR
using System;
using Ami.BroAudio.Data;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        [System.Obsolete("Only for backwards compatibility")]
        public void AssignCoreData(BroAudioData coreData)
        {
            if(_data != coreData)
            {
                _data = coreData;
                PrefabUtility.SavePrefabAsset(gameObject);
            }
        }
	}
}
#endif
