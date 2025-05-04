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
    public class BroAudioData : ScriptableObject
    {
        public const string CodeBaseVersion = "2.0.6";

        [SerializeField, ReadOnly] string _version;
        [SerializeField] List<AudioAsset> _assets = new List<AudioAsset>();
        
        public IReadOnlyList<IAudioAsset> Assets => _assets;
        // 1.15 is the last version without this version control mechanic
        public Version Version => string.IsNullOrEmpty(_version) ? new Version(1,15) : new Version(_version);

#if UNITY_EDITOR
        public List<string> GetGUIDList()
        {
            List<string> list = new List<string>();
            foreach (var asset in _assets)
            {
                list.Add(asset.AssetGUID);
            }
            return list;
        }

        public void AddAsset(AudioAsset asset)
        {
            if(asset)
            {
                _assets.Add(asset);
            }	
        }

        public bool RemoveEmpty()
        {
            bool hasRemoved = false;
            for (int i = _assets.Count - 1; i >= 0; i--)
            {
                if (!_assets[i])
                {
                    _assets.RemoveAt(i);
                    hasRemoved = true;
                }
            }
            return hasRemoved;
        }

        public void ReorderAssets(List<string> guids)
        {
            if (_assets.Count != guids.Count)
            {
                Debug.LogError(Utility.LogTitle + "Asset count is not match!");
                return;
            }
            _assets = _assets.OrderBy(x => guids.IndexOf(x.AssetGUID)).ToList();
        } 

        public void UpdateVersion()
        {
            _version = CodeBaseVersion;
        }
#endif
    } 
}