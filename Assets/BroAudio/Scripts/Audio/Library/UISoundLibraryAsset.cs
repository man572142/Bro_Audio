using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	[CreateAssetMenu(fileName = "UISoundLibrary", menuName = "MiProduction/BroAudio/Create Library/UI")]
	public class UISoundLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.UI;
	} 
}
