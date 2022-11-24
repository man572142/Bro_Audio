using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;
using System;
using System.Linq;

namespace MiProduction.BroAudio.Library
{
    public abstract class AudioLibraryAsset<T> : ScriptableObject, IAudioLibraryAsset where T : IAudioLibrary
    {
        public T[] Libraries;

        //IAudioLibrary[] IAudioLibraryAsset.Libraries
        //{
        //    get
        //    {
        //        //List<IAudioLibrary> tempLibraries = new List<IAudioLibrary>();
        //        //foreach (T library in Libraries)
        //        //{
        //        //    tempLibraries.Add(library);
        //        //}
        //        //return tempLibraries.ToArray();
        //        return Libraries.Cast<IAudioLibrary>().ToArray();
        //    }
        //}

        // Do Not Delete This Line
        [SerializeField, HideInInspector] private string _enumsPath = string.Empty;
        public abstract string LibraryTypeName { get; }

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