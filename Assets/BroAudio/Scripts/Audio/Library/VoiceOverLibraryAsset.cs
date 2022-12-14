using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "VoiceOverLibrary", menuName = "MiProduction/BroAudio/Create Library/VoiceOver")]
	public class VoiceOverLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.VoiceOver;
	} 
}
