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

        // Do Not Delete This Line
        [SerializeField, HideInInspector] private string _enumsPath = string.Empty;

        private string _assetGUID = string.Empty;
        private string _libraryName = string.Empty;
        
		public abstract AudioType AudioType { get; }

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
        }

        /// <summary>
        /// 每次Get都要跑Linq，若要在Loop中使用建議先暫存起來
        /// </summary>
        public string[] AllAudioDataNames
        {
            get
            {
                if (Sets == null)
                    Sets = new T[0];

                return Sets.Select(x => x.EnumName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
            }
        }
	}
}