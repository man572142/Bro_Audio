using System.Collections.Generic;
using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class ShuffleClipStrategy : IClipSelectionStrategy
    {
        private BroAudioClip _lastUsed;
        private readonly HashSet<BroAudioClip> _used = new HashSet<BroAudioClip>();

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
                    
                    if (!_used.Contains(clip))
                    {
                        hasAnyAvailable = true;
                    }
                }
                
                if(!hasAnyAvailable)
                {
                    Reset();
                    _lastUsed = result;
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
                    if(!_used.Contains(clips[index])) // if there are any available clips, return. Otherwise, proceed to ResetInUse
                    {
                        return result;
                    }
                }
                else if (Use(clips, index, out result))
                {
                    checkRanOut = true;
                }
            }

            _lastUsed = result;
            Reset();
            return result;
        }

        private bool Use(BroAudioClip[] clips, int index, out BroAudioClip result, bool checkLastUsed = false)
        {
            result = clips[index];
            if (result == _lastUsed && checkLastUsed)
            {
                _lastUsed = null;
                return false;
            }
            
            if (result != _lastUsed && result.IsSet)
            {
                _used.Add(result);
                return true;
            }
            result = null;
            return false;
        }

        public void Reset()
        {
            _used.Clear();
        }
    }
}
