using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;
using System;
using System.Linq;
using UnityEditor;

namespace MiProduction.BroAudio.Data
{
    public abstract class AudioAsset<T> : ScriptableObject, IAudioAsset where T : IAudioEntity
    {

        public T[] Libraries;

		public abstract AudioType AudioType { get; }
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

        public IEnumerable<IAudioEntity> GetAllAudioEntities()
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