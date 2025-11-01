using System;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public class ChainedClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = Math.Max(context.Value - 1, 0);
            if (index < 0 || index >= clips.Length)
            {
                throw new IndexOutOfRangeException($"There's no clip for Chained Play Mode Stage:<b>{(PlaybackStage)context.Value}</b>");
            }
            return clips[index];
        }
    }
}