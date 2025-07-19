using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class ShuffleClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = Random.Range(0, clips.Length);
            BroAudioClip result;

            if(Use(clips, index, out result))
            {
                if(!HasAnyAvailable(clips))
                {
                    ResetIsUse(clips, result);
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
                else if (Use(clips, index, out result))
                {
                    checkRanOut = true;
                }
            }

            ResetIsUse(clips, result);
            return result;
        }

        private static bool HasAnyAvailable(BroAudioClip[] clips)
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

        private static bool Use(BroAudioClip[] clips, int index, out BroAudioClip result)
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

        public static void ResetIsUse(BroAudioClip[] clips, BroAudioClip exclude = null)
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
    }
}
