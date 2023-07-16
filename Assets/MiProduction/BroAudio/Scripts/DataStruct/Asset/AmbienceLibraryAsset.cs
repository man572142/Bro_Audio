using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	public class AmbienceLibraryAsset : AudioAsset<PersistentAudioLibrary>
	{
		public override BroAudioType AudioType => BroAudioType.Ambience;
	}
}
