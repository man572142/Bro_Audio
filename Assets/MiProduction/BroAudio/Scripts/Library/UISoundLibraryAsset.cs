using UnityEngine;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(fileName = "UISoundLibrary", menuName = "MiProduction/BroAudio/Library/UI")]
	public class UISoundLibraryAsset : SoundLibraryAsset
	{
		public override AudioType AudioType => AudioType.UI;
	} 
}
