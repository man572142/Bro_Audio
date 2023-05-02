using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(fileName = "AmbienceLibrary", menuName = "MiProduction/BroAudio/Library/Ambience")]
	public class AmbienceLibraryAsset : AudioAsset<Data.MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Ambience;
	}
}
