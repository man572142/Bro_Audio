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

        private string _assetGUID = string.Empty;

        public string AssetGUID
		{
			get
			{
                if(string.IsNullOrEmpty(_assetGUID))
				{
                    _assetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));
                    Debug.Log(_assetGUID);
                }
                return _assetGUID;
			}
		}

        // Do Not Delete This Line
        [SerializeField, HideInInspector] private string _enumsPath = string.Empty;
        public abstract string LibraryTypeName { get; }

		public abstract AudioType AudioType { get; }


		string[] IAudioLibraryAsset.AllLibraryEnumNames
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