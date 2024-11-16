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
        public const string CodeBaseVersion = "1.15";

        [SerializeField, ReadOnly] string _version = "1.0";
        [SerializeField] List<AudioAsset> _assets = new List<AudioAsset>();
        
		public IReadOnlyList<AudioAsset> Assets => _assets;
        public Version Version => new Version(_version);

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

		public void RemoveEmpty()
		{
			for (int i = _assets.Count - 1; i >= 0; i--)
			{
				if (!_assets[i])
				{
					_assets.RemoveAt(i);
				}
			}
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