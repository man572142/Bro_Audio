using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "AmbienceLibrary", menuName = "MiProduction/BroAudio/Library/Ambience")]
	public class AmbienceLibraryAsset : AudioLibraryAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Ambience;
	}
}
