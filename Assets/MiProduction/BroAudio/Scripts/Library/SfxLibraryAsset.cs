using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library.Core
{
	[CreateAssetMenu(fileName = "SoundLibrary", menuName = "MiProduction/BroAudio/Library/Sound")]
	public class SfxLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.SFX;
	} 
}
