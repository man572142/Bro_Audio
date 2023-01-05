using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library.Core
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "MiProduction/BroAudio/Library/Music")]
	public class MusicLibraryAsset : AudioLibraryAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Music;
	}
}
