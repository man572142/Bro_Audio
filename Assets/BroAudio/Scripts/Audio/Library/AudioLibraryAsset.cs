using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;
using System;
using System.Linq;
using UnityEditor;

namespace MiProduction.BroAudio.Library
{
    public abstract class AudioLibraryAsset<T> : ScriptableObject, IAudioLibraryAsset where T : IAudioLibrary
    {
        public T[] Libraries;

        // Do Not Delete This Line
        [SerializeField, HideInInspector] private string _enumsPath = string.Empty;

        private string _assetGUID = string.Empty;
        
        public abstract string LibraryTypeName { get; }
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
                if (Libraries == null)
                    Libraries = new T[0];

                return Libraries.Select(x => x.EnumName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
            }
        }
	}
}