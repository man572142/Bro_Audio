using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Asset
{
	public abstract class AudioLibraryAsset<T> : ScriptableObject,IAudioLibraryIdentify where T : IAudioLibrary
	{
		public T[] Libraries;

		public string LibraryTypeName => typeof(T).Name.Replace("Library","");

		IAudioLibrary[] IAudioLibraryIdentify.Libraries 
		{
			get
			{
				List<IAudioLibrary> tempLibraries = new List<IAudioLibrary>();
				foreach(T library in Libraries)
				{
					tempLibraries.Add((IAudioLibrary)library);
				}
				return tempLibraries.ToArray();
			}
		} 
		
		//public string GetNameByIndex(int index) => Libraries[index].Name;
	}
}