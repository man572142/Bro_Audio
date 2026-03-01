using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Data
{
#if BroAudio_DevOnly
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/BroAudioData", fileName = "BroAudioData")]
#endif
    [System.Obsolete("No need for this to exist anymore")]
    public class BroAudioData : ScriptableObject
    {
        [System.Obsolete("No need for this to exist anymore")]
        [SerializeField, ReadOnly] public string _version;

        [System.Obsolete("No need for this to exist anymore", true)]
        [SerializeField] List<AudioAsset> _assets = new List<AudioAsset>();

        [System.Obsolete("No need for this to exist anymore")]
        public IReadOnlyList<IAudioAsset> Assets => _assets;
        // 1.15 is the last version without this version control mechanic
    } 
}