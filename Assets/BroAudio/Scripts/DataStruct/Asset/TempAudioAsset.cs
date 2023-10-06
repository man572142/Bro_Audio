using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Data
{
	public class TempAudioAsset : AudioAsset<AudioLibrary>
	{
		public override BroAudioType AudioType => BroAudioType.None;
	}
}