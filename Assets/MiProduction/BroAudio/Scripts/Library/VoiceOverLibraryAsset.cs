using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(fileName = "VoiceOverLibrary", menuName = "MiProduction/BroAudio/Library/VoiceOver")]
	public class VoiceOverLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.VoiceOver;
	} 
}
