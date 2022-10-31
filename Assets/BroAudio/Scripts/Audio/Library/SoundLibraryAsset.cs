using MiProduction.BroAudio.Library;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "SoundLibrary", menuName = "MiProduction/BroAudio/Create Library/Sound")]
	public class SoundLibraryAsset : AudioLibraryAsset<SoundLibrary>
	{
		public override string LibraryTypeName => "Sound";
	}
}
