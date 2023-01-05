using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library.Core
{
	[CreateAssetMenu(fileName = "UISoundLibrary", menuName = "MiProduction/BroAudio/Library/UI")]
	public class UISoundLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.UI;
	} 
}
