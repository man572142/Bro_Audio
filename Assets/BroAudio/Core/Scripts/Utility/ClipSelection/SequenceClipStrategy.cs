using System.Collections.Generic;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    /// <summary>
    /// Strategy for playing clips in sequential order
    /// </summary>
    public class SequenceClipStrategy : IClipSelectionStrategy
    {
        private static Dictionary<int, int> _sequencer = new Dictionary<int, int>();

        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int currentIndex)
        {
            _sequencer ??= new Dictionary<int, int>();

            if(_sequencer.TryGetValue(context.Id, out currentIndex))
            {
                int nextIndex = currentIndex + 1;
                nextIndex = nextIndex >= clips.Length ? 0 : nextIndex;
                if (clips[nextIndex].GetAudioClip() != null)
                {
                    _sequencer[context.Id] = nextIndex;
                    currentIndex = nextIndex;
                }
            }
            else if(clips[0].GetAudioClip() != null)
            {
                _sequencer.Add(context.Id, 0);
            }
            return clips[currentIndex];
        }

        public static void Reset(int id)
        {
            _sequencer?.Remove(id);
        }

        public static void ResetAll()
        {
            _sequencer?.Clear();
        }

#if UNITY_EDITOR
        public static void ClearSequencer()
        {
            _sequencer?.Clear();
            _sequencer = null;
        }
#endif
    }
}
