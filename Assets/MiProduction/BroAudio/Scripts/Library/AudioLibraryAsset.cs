using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;
using System;
using System.Linq;
using UnityEditor;

namespace MiProduction.BroAudio.Library.Core
{
    public abstract class AudioLibraryAsset<T> : ScriptableObject, IAudioLibraryAsset where T : IAudioLibrary
    {
        public T[] Sets;

		public abstract AudioType AudioType { get; }

        [SerializeField] private string _libraryName = string.Empty;
        public string LibraryName { get => _libraryName;  set => _libraryName = value; }

        [SerializeField] private string _assetGUID = string.Empty;
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

		public IEnumerable<AudioData> AllAudioData
		{
			get
			{
				if (Sets == null)
					Sets = new T[0];

				foreach (var data in Sets)
				{
					yield return new AudioData(data.ID, data.EnumName);
				}
			}
		}
	}
}