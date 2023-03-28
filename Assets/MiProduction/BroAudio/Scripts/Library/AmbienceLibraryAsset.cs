using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset.Core
{
	[CreateAssetMenu(fileName = "AmbienceLibrary", menuName = "MiProduction/BroAudio/Library/Ambience")]
	public class AmbienceLibraryAsset : AudioAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Ambience;
	}
}
