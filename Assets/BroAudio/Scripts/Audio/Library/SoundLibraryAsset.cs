using MiProduction.BroAudio.Library;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	
	public abstract class SoundLibraryAsset : AudioLibraryAsset<SoundLibrary>
	{
		public override string LibraryTypeName => SoundLibraryTypeName;

		public abstract string SoundLibraryTypeName { get; }
	}

}
