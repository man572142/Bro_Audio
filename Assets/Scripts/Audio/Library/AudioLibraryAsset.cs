using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
	public class AudioLibraryAsset<T> : ScriptableObject where T : IAudioLibrary
	{
		public T[] Libraries;
	}
}