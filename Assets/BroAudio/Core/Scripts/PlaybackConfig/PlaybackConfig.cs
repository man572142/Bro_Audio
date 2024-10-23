using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Data
{
    public abstract class PlaybackConfig : ScriptableObject
    {
        [field: SerializeField]
        public int MaxPlayableCount { get; private set; }

        public int CurrentPlayingCount { get; private set; }

        protected abstract bool VerifyCombFiltering();
        protected abstract bool VerifyDistance();

        public virtual bool IsPlayable()
        {
            if (CurrentPlayingCount >= MaxPlayableCount)
            {
                return false;
            }

            if(!VerifyCombFiltering())
            {
                return false;
            }

            if(!VerifyDistance())
            {
                return false;
            }

            // TODO: And More?
            return true;
        }

        public void AddPlayingEntity(IAudioPlayer player)
        {
            CurrentPlayingCount++;
            player.OnEnd(x => CurrentPlayingCount--);
        }

        public void Update()
        {
            // 如果超出範圍，停止播放，重新進入範圍，再次播放
        }
    }
}
