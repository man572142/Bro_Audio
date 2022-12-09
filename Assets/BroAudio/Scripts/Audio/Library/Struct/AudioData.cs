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
		public AudioType AudioType;
		public string AssetGUID;

		public AudioData(int id, string name, AudioType audioType, string assetGUID)
		{
			ID = id;
			AudioType = audioType;
			Name = name;
			AssetGUID = assetGUID;
		}
	} 
}
