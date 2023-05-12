using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public class MusicLibrary : AudioLibrary
    {
		public bool Loop;

		protected override string DisplayName => nameof(MusicLibrary);
	}
}