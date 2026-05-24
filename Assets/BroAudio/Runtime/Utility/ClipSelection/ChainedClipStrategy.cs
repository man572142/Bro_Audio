using System;
using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class ChainedClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = Math.Max(context.Value - 1, 0);
            if (Utility.ClipListIsNullOrEmpty(clips, context.EntityName))
            {
                return null;
            }
            if (index < 0 || index >= clips.Length)
            {
                Debug.LogError(Utility.LogTitle + $"There's no clip for Chained Play Mode Stage:<b>{(PlaybackStage)context.Value}</b> in {context.EntityName}");
                return null;
            }
            return clips[index];
        }

        public void Reset() { }
    }
}