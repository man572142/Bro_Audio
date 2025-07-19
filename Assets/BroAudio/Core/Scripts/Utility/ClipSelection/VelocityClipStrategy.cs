using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public class VelocityClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = 0;
            for(int i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if(clip.Velocity > context.Value)
                {
                    index = i == 0 ? 0 : i - 1;
                    return clips[index];
                }
            }
            return clips.Length > 0 ? clips[clips.Length - 1] : null;
        }
    }
}
