using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public class LayeredClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = 0;
            if (Utility.ClipListIsNullOrEmpty(clips, context.EntityName))
            {
                return null;
            }
            return clips[index];
        }

        public void Reset() { }
    }
}