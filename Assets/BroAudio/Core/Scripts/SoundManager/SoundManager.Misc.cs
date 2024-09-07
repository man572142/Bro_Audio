using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using System;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        public AudioClip GetAudioClip(SoundID id)
        {
            return GetAudioClip(id, x => x.PickNewClip());
        }

        public AudioClip GetAudioClip(SoundID id, int velocity)
        {
            return GetAudioClip(id, x => x.PickNewClip(velocity));
        }

        private AudioClip GetAudioClip(SoundID id, Func<IAudioEntity, BroAudioClip> onGetAudioClip)
        {
            if (_audioBank.TryGetValue(id, out var entity))
            {
                var broClip = onGetAudioClip?.Invoke(entity);
                if (broClip != null)
                {
                    return broClip.AudioClip;
                }
            }
            return null;
        }

        public void ResetShuffleInUseState(int id)
        {
            if(_audioBank.TryGetValue(id, out IAudioEntity entity))
            {
                entity.ResetShuffleInUseState();
            }
        }

        public void ResetShuffleInUseState()
        {
            foreach(var entity in _audioBank.Values)
            {
                entity.ResetShuffleInUseState();
            }
        }

        public string GetNameByID(int id)
        {
            if(!IsAvailable())
            {
                return string.Empty;
            }

            string result = string.Empty;
            if(_audioBank.TryGetValue(id,out var entity))
            {
                IEntityIdentity entityIdentity = entity as IEntityIdentity;
                result = entityIdentity?.Name;
            }
            return result;
        }

        public bool IsIdInBank(SoundID id)
        {
            return _audioBank.ContainsKey(id);
        }

        private bool IsAvailable(bool logError = true)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError(LogTitle + $"The method {"GetNameByID".ToWhiteBold()} is {"Runtime Only".ToBold().SetColor(Color.green)}");
                return false;
            }
            return true;
        }
    }
}