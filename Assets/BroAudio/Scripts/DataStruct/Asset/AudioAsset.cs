using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.BroAudio.Data
{
    public class AudioAsset : ScriptableObject, IAudioAsset
    {
        public AudioLibrary[] Libraries;
        public virtual bool IsTemp => false;

        [field: SerializeField] public BroAudioType AudioType { get; set; }
        [field: SerializeField] public string AssetName { get; set; }

        [SerializeField] private string _assetGUID;
        public string AssetGUID
        {
            get
            {
                if (string.IsNullOrEmpty(_assetGUID))
                {
                    _assetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
                }
                return _assetGUID;
            }
			set
			{
                _assetGUID = value;
			}
        }

        public IEnumerable<IAudioLibrary> GetAllAudioLibraries()
		{
            if (Libraries == null)
                Libraries = new AudioLibrary[0];

            foreach (var data in Libraries)
            {
                yield return data;
            }
        }
	}
}