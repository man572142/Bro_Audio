using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "SoundLibrary", menuName = "MiProduction/BroAudio/Create Library/Sound")]
	public class SfxLibraryAsset : SoundLibraryAsset
	{
		public override string SoundLibraryTypeName { get => "Sound"; }
	} 
}
