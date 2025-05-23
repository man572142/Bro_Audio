using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        private static Dictionary<int, int> ClipsSequencer = null;
        
        public static void ResetClipSequencer(int id)
        {
            ClipsSequencer?.Remove(id);
        }

        public static void ResetClipSequencer()
        {
            ClipsSequencer?.Clear();
        }

        public static IBroAudioClip PickNewOne(this BroAudioClip[] clips, MulticlipsPlayMode playMode, int id, out int index, int velocity = 0)
        {
            index = 0;
            if (clips == null || clips.Length <= 0)
            {
                Debug.LogError(LogTitle + "There are no AudioClip in the entity");
                return null;
            }
            else if (clips.Length == 1)
            {
                playMode = MulticlipsPlayMode.Single;
            }

            return playMode switch
            {
                MulticlipsPlayMode.Single => clips[0],
                MulticlipsPlayMode.Sequence => clips.PickNextClip(id, out index),
                MulticlipsPlayMode.Random => clips.PickRandomClip(out index),
                MulticlipsPlayMode.Shuffle => clips.PickShuffleClip(out index),
                MulticlipsPlayMode.Velocity => clips.PickClipByVelocity(velocity, out index),
                _ => default,
            };
        }

        private static BroAudioClip PickNextClip(this BroAudioClip[] clips, int id, out int currentIndex)
        {
            ClipsSequencer ??= new Dictionary<int, int>();

            if(ClipsSequencer.TryGetValue(id, out currentIndex))
            {
                int nextIndex = currentIndex + 1;
                nextIndex = nextIndex >= clips.Length ? 0 : nextIndex;
                if (clips[nextIndex].GetAudioClip() != null)
                {
                    ClipsSequencer[id] = nextIndex;
                    currentIndex = nextIndex;
                }          
            }
            else if(clips[0].GetAudioClip() != null)
            {
                ClipsSequencer.Add(id, 0);
            }
            return clips[currentIndex];
        }

        private static BroAudioClip PickRandomClip(this BroAudioClip[] clips, out int index)
        {
            index = 0;
            int totalWeight = 0;
            foreach (var clip in clips)
            {
                totalWeight += clip.Weight;
            }

            // No Weight
            if (totalWeight == 0)
            {
                index = Random.Range(0, clips.Length);
                return clips[index];
            }

            // Use Weight
            int targetWeight = Random.Range(0, totalWeight);
            int sum = 0;

            for (int i = 0; i < clips.Length; i++)
            {
                sum += clips[i].Weight;
                if (targetWeight < sum)
                {
                    index = i;
                    return clips[i];
                }
            }
            return default;
        }

        private static BroAudioClip PickShuffleClip(this BroAudioClip[] clips, out int index)
        {
            index = Random.Range(0, clips.Length);
            BroAudioClip result;

            if(Use(index, out result))
            {
                if(!HasAnyAvailable())
                {
                    clips.ResetIsUse(result);
                }
                return result;
            }

            // to avoid overusing the Random method when there are only a few clips left
            int increment = Random.Range(0,2) == 0 ? -1 : 1;
            bool checkRanOut = false;
            for(int i = 0; i < clips.Length; i++)
            {
                index += increment;
                index = (index + clips.Length) % clips.Length;

                if (checkRanOut) 
                {
                    if(!clips[index].IsUsed) // if there are any available clips, return. Otherwise, proceed to ResetInUse
                    {
                        return result;
                    }
                }
                else if (Use(index, out result))
                {
                    checkRanOut = true;
                }
            }

            clips.ResetIsUse(result);
            return result;

            bool HasAnyAvailable()
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (!clips[i].IsUsed)
                    {
                        return true;
                    }
                }
                return false;
            }

            bool Use(int index, out BroAudioClip result)
            {
                result = clips[index];
                if (!result.IsUsed && result.GetAudioClip() != null)
                {
                    result.IsUsed = true;
                    return true;
                }
                result = null;
                return false;
            }
        }

        public static IBroAudioClip PickClipByVelocity(this BroAudioClip[] clips, int velocity, out int index)
        {
            index = 0;
            for(int i = 0; i < clips.Length;i++)
            {
                var clip = clips[i];
                if(clip.Velocity > velocity)
                {
                    index = i == 0 ? 0 : i - 1;
                    return clips[index];
                }
            }
            return clips.Length > 0 ? clips[clips.Length - 1] : null;
        }

        public static void ResetIsUse(this BroAudioClip[] clips, BroAudioClip exclude = null)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip != exclude)
                {
                    clips[i].IsUsed = false;
                }
            }
        }

#if UNITY_EDITOR
        public static void ClearClipsSequencer()
        {
            ClipsSequencer?.Clear();
            ClipsSequencer = null;
        }
#endif
    }
}