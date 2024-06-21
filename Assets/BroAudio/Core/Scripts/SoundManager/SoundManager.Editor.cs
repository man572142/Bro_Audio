#if UNITY_EDITOR
using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        public void AssignCoreData(BroAudioData coreData)
        {
			_data = coreData;
        }
	}
}
#endif
