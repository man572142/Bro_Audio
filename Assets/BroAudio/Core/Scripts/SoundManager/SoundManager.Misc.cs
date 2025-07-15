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

        public AudioClip GetAudioClip(SoundID id, PlaybackStage chainedModeStage)
        {
            return GetAudioClip(id, x => x.PickNewClip((int)chainedModeStage));
        }

        private AudioClip GetAudioClip(SoundID id, Func<IAudioEntity, IBroAudioClip> onGetAudioClip)
        {
            if (TryGetEntity(id, out var entity))
            {
                var broClip = onGetAudioClip?.Invoke(entity);
                if (broClip != null)
                {
                    return broClip.GetAudioClip();
                }
            }
            return null;
        }

        public void ResetShuffleInUseState(int id)
        {
            if(TryGetEntity(id, out IAudioEntity entity))
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
            if(TryGetEntity(id,out var entity))
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

        public bool TryGetEntity(SoundID id, out IAudioEntity entity, bool logError = true)
        {
            entity = null;
            if (logError)
            {
                if (id == 0)
                {
                    Debug.LogError($"The SoundID hasn't been assigned yet! {GetDebugObjectName()}", GetDebugObject());
                    return false;
                }
                else if (id == SoundID.Invalid)
                {
                    Debug.LogError($"The SoundID:{id} is invalid! {GetDebugObjectName()}", GetDebugObject());
                    return false;
                }
                else if (!_audioBank.TryGetValue(id, out entity))
                {
                    Debug.LogError($"Missing audio entity for SoundID: {id}! {GetDebugObjectName()}", GetDebugObject());
                    return false;
                }
                return true;
            }
            return id > 0 && _audioBank.TryGetValue(id, out entity);

            string GetDebugObjectName()
            {
                var obj = GetDebugObject();
                if (obj != null)
                {
                    return $"Source:{obj.name.ToBold()}";
                }
                return string.Empty;
            }

#if UNITY_EDITOR
            GameObject GetDebugObject() => id.DebugObject;
#else
            GameObject GetDebugObject() => null;
#endif
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