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
        private Dictionary<string, int> _namedSequenceIndices;

        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int currentIndex)
        {
            if (context.SequenceId != null)
            {
                return SelectClipForNamedSequence(clips, context.SequenceId, out currentIndex);
            }

            return SelectClipForDefaultSequence(clips, out currentIndex);
        }

        private IBroAudioClip SelectClipForDefaultSequence(BroAudioClip[] clips, out int currentIndex)
        {
            int nextIndex = 0;

            if (_sequenceIndex > -1)
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

        private IBroAudioClip SelectClipForNamedSequence(BroAudioClip[] clips, string sequenceId, out int currentIndex)
        {
            _namedSequenceIndices ??= new Dictionary<string, int>();

            _namedSequenceIndices.TryGetValue(sequenceId, out int seqIndex);
            // TryGetValue returns 0 for missing keys; we use -1 as uninitialized, so default to -1
            if (!_namedSequenceIndices.ContainsKey(sequenceId))
            {
                seqIndex = -1;
            }

            int nextIndex = 0;

            if (seqIndex > -1)
            {
                nextIndex = seqIndex + 1;
                nextIndex = nextIndex >= clips.Length ? 0 : nextIndex;
            }
            else if (clips[0].IsSet)
            {
                nextIndex = 0;
            }

            if (clips[nextIndex].IsSet)
            {
                _namedSequenceIndices[sequenceId] = nextIndex;
                currentIndex = nextIndex;
            }
            else
            {
                _namedSequenceIndices[sequenceId] = -1;
                currentIndex = -1;
            }

            return clips[_namedSequenceIndices[sequenceId]];
        }

        public void Reset()
        {
            _sequenceIndex = -1;
            _namedSequenceIndices?.Clear();
        }

        public void Reset(string sequenceId)
        {
            if (sequenceId == null)
            {
                _sequenceIndex = -1;
                return;
            }

            if (_namedSequenceIndices != null)
            {
                _namedSequenceIndices.Remove(sequenceId);
            }
        }
    }
}
