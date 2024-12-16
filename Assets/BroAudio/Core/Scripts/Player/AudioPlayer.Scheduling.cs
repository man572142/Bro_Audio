using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
	{
        IAudioPlayer ISchedulable.SetScheduledStartTime(double dspTime)
        {
            if(_pref.ScheduledStartTime <= 0d)
            {
                _pref.ScheduledStartTime = dspTime;
            }
            else
            {
                AudioSource.SetScheduledStartTime(dspTime);
                _timeBeforeStartSchedule += (float)(dspTime - _pref.ScheduledStartTime);
            }
            return this;
        }

        IAudioPlayer ISchedulable.SetScheduledEndTime(double dspTime)
        {
            AudioSource.SetScheduledEndTime(dspTime);
            _pref.ScheduledEndTime = dspTime;
            _onUpdate -= CheckScheduledEnd;
            _onUpdate += CheckScheduledEnd;
            return this;
        }

        private void CheckScheduledEnd(IAudioPlayer player)
        {
            if(AudioSettings.dspTime >= _pref.ScheduledEndTime)
            {
                this.SafeStopCoroutine(_playbackControlCoroutine);
                _onUpdate -= CheckScheduledEnd;
            }
        }

        IAudioPlayer ISchedulable.SetDelay(float delay)
        {
            if(AudioSource.isPlaying)
            {
                Debug.LogWarning(Utility.LogTitle + "SetDelay failed! The AudioPlayer is already playing or has been scheduled");
                return this;
            }

            this.SetScheduledStartTime(AudioSettings.dspTime + delay);
            return this;
        }
    }
}
