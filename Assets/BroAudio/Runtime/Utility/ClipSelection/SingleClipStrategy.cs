using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Strategy for selecting the first clip in the array
    /// </summary>
    public class SingleClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = 0;
            return clips[0];
        }

        public void Reset() { }
    }
}
