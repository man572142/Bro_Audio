using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio.Data
{
    public abstract class AudioAsset<T> : ScriptableObject, IAudioAsset where T : IAudioLibrary
    {

        public T[] Libraries;

		public abstract BroAudioType AudioType { get; }
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
                Libraries = new T[0];

            foreach (var data in Libraries)
            {
                yield return data;
            }
        }
	}
}