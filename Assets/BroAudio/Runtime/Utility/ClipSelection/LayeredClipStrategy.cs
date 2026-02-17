using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public class LayeredClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            throw new System.NotImplementedException();
        }

        public void Reset() { }
    }
}