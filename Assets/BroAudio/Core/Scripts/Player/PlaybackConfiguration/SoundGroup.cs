using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/Sound Group", fileName = "SoundGroup", order = 0)]
    public class SoundGroup : ScriptableObject
    {
        [field: SerializeField]
        public int MaxPlayableCount { get; private set; }
        public int CurrentPlayingCount { get; private set; }

        public bool VerifyPlayableAndAddCount(IAudioPlayer player)
        {
            if(CurrentPlayingCount >= MaxPlayableCount)
            {
                return false;
            }

            CurrentPlayingCount++;
            player.OnEnd(_ => CurrentPlayingCount--);
            return true;
        }

        private void OnEnable()
        {
            CurrentPlayingCount = 0;
        }
    } 
}
