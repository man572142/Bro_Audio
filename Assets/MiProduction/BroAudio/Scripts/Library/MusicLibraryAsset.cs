using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset.Core
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "MiProduction/BroAudio/Library/Music")]
	public class MusicLibraryAsset : AudioAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Music;
	}
}
