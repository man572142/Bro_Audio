using MiProduction.BroAudio.Library;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "MusicLibrary", menuName = "MiProduction/BroAudio/Library/Music")]
	public class MusicLibraryAsset : AudioLibraryAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Music;
	}
}
