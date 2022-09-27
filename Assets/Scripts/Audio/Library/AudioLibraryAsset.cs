using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;
using System;
using System.Linq;

namespace MiProduction.BroAudio.Library
{
    public abstract class AudioLibraryAsset<T> : ScriptableObject, IAudioLibraryIdentify where T : IAudioLibrary
    {
        public T[] Libraries;

        public string LibraryTypeName => typeof(T).Name.Replace("Library", "");

        [SerializeField, HideInInspector] private string _enumsPath = string.Empty;

        string[] IAudioLibraryIdentify.AllLibraryEnumNames
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