using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
	{
        private bool _isScheduled = false;

        public IAudioPlayer SetScheduledStartTime(double dspTime)
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

        public IAudioPlayer SetScheduledEndTime(double dspTime)
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

        public IAudioPlayer SetDelay(float delay)
        {
            if(AudioSource.isPlaying)
            {
                Debug.LogWarning(Utility.LogTitle + "SetDelay failed! The AudioPlayer is already playing or has been scheduled");
                return this;
            }

            SetScheduledStartTime(AudioSettings.dspTime + delay);
            return this;
        }
    }
}
