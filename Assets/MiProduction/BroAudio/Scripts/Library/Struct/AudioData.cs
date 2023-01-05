using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	[System.Serializable]
	public struct AudioData
	{
		public int ID;
		public string Name;
		public string LibraryName;
		public string AssetGUID;

		public AudioData(int id, string name, string libraryName, string assetGUID)
		{
			ID = id;
			LibraryName = libraryName;
			Name = name;
			AssetGUID = assetGUID;
		}
	} 
}
