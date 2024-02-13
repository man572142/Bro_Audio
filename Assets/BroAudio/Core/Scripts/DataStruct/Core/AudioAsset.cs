using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.BroAudio.Data
{
    public class AudioAsset : ScriptableObject, IAudioAsset
    {
        public AudioEntity[] Entities;

        [field: SerializeField] public string AssetName { get; set; }

        [SerializeField] private string _assetGUID;
#if UNITY_EDITOR
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
#endif

        public IEnumerable<IEntityIdentity> GetAllAudioEntities()
		{
            Entities = Entities ?? new AudioEntity[0];

            foreach (var data in Entities)
            {
                yield return data;
            }
        }
	}
}