using System.Collections.Generic;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Strategy for playing clips in sequential order
    /// </summary>
    public class SequenceClipStrategy : IClipSelectionStrategy
    {
        private int _sequenceIndex = -1;

        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int currentIndex)
        {
            int nextIndex = 0;

            if(_sequenceIndex > -1)
            {
                nextIndex = _sequenceIndex + 1;
                nextIndex = nextIndex >= clips.Length ? 0 : nextIndex;
            }
            else if (clips[0].IsSet)
            {
                nextIndex = 0;
            }

            if (clips[nextIndex].IsSet)
            {
                _sequenceIndex = nextIndex;
                currentIndex = nextIndex;
            }
            else
            {
                _sequenceIndex = -1;
                currentIndex = -1;
            }

            return clips[_sequenceIndex];
        }

        public void Reset()
        {
            _sequenceIndex = -1;
        }
    }
}
