using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.BroAudio.Data
{
    public class AudioAsset : ScriptableObject, IAudioAsset
    {
        public AudioEntity[] Entities;
        public PlaybackGroup Group;

        private PlaybackGroup _upperGroup;

        public PlaybackGroup PlaybackGroup => Group ? Group : _upperGroup;

        public int EntitiesCount => Entities.Length;

#if UNITY_EDITOR
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
#endif

        public IEnumerable<IEntityIdentity> GetAllAudioEntities()
		{
            Entities ??= System.Array.Empty<AudioEntity>();

            foreach (var data in Entities)
            {
                yield return data;
            }
        }


        public void LinkPlaybackGroup(PlaybackGroup upperGroup)
        {
            if (Group != null)
            {
                Group.SetParent(upperGroup);         
            }
            else
            {
                _upperGroup = upperGroup;
            }
        }
    }
}