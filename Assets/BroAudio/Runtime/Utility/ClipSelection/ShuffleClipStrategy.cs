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

            if(Use(clips, index, out result, true))
            {
                bool hasAnyAvailable = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];
                    clip.IsLastUsed = false;
                    
                    if (!clip.IsUsed)
                    {
                        hasAnyAvailable = true;
                    }
                }
                
                if(!hasAnyAvailable)
                {
                    ResetIsUse(clips);
                    result.IsLastUsed = true;
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

            result.IsLastUsed = true;
            ResetIsUse(clips);
            return result;
        }

        private static bool Use(BroAudioClip[] clips, int index, out BroAudioClip result, bool checkLastUsed = false)
        {
            result = clips[index];
            if (result.IsLastUsed && checkLastUsed)
            {
                result.IsLastUsed = false;
                return false;
            }
            
            if (!result.IsUsed && result.GetAudioClip() != null)
            {
                result.IsUsed = true;
                return true;
            }
            result = null;
            return false;
        }

        public static void ResetIsUse(BroAudioClip[] clips)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].IsUsed = false;
            }
        }
    }
}
