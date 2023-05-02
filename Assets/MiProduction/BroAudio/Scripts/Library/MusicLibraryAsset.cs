using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "MiProduction/BroAudio/Library/Music")]
	public class MusicLibraryAsset : AudioAsset<Data.MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Music;
	}
}
