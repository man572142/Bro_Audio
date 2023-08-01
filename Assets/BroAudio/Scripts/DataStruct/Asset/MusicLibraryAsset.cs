using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Data
{
	public class MusicLibraryAsset : AudioAsset<PersistentAudioLibrary>
	{
		public override BroAudioType AudioType => BroAudioType.Music;
	}
}
