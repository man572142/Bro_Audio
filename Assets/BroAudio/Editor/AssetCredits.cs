using Ami.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
#if BroAudio_DevOnly
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/Asset Credits", fileName = "AssetCredits")] 
#endif
    public class AssetCredits : ScriptableObject
    {
        public enum AssetType
        {
            Audio, Texture, Model, Shader, Script,
        }


		public const bool CanEdit = false;

        [System.Serializable]
        public struct Credit
        {
            public Object Source;
            public AssetType Type;
			[ReadOnlyTextArea(!CanEdit)]
			public string Name;
            [ReadOnlyTextArea(!CanEdit)]
            public string Author;
            [ReadOnlyTextArea(!CanEdit)]
            public string License;
            [ReadOnlyTextArea(!CanEdit)]
            public string Link;
        }

        public Credit[] Credits;
    }
}
