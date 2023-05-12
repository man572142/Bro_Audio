using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	public class AmbienceLibraryAsset : AudioAsset<MusicLibrary>
	{
		public override AudioType AudioType => AudioType.Ambience;
	}
}
