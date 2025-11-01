using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public class ReplayRequest
    {
        public IBroAudioClip Clip { get; protected set; }
        public virtual float MasterVolume =>AudioConstant.FullVolume;
        public virtual float Pitch => AudioConstant.DefaultPitch;
        public int StartSample => Clip.GetAudioClip().GetTimeSample(Clip.StartPosition);

        public ReplayRequest(IBroAudioClip clip)
        {
            Clip = clip;
        }

        public virtual bool CanReplay()
        {
            return true;
        }
        
        public virtual AudioClip GetAudioClipForScheduling()
        {
            return Clip?.GetAudioClip();
        }

        public virtual void Start()
        {
        }

        public double GetDuration()
        {
            if (Clip == null)
            {
                return 0;
            }

            var length = Clip.GetAudioClip().GetPreciseLength();
            return (length - Clip.StartPosition - Clip.EndPosition) / Pitch;
        }
    }
}
