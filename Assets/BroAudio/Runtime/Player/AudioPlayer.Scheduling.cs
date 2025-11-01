using UnityEngine;
using Ami.Extension;
using System.Collections;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
	{
        private float _timeBeforeStartSchedule = 0f;

        private void SetScheduleTime(out bool isSchdeuledPlay)
        {
            isSchdeuledPlay = false;
            if (_pref.ScheduledStartTime > 0d) // Scheduled has higher priority than clip.delay
            {
                AudioSource.PlayScheduled(_pref.ScheduledStartTime);
                _timeBeforeStartSchedule = (float)(_pref.ScheduledStartTime - AudioSettings.dspTime);
                isSchdeuledPlay = true;
            }
            else if (_clip.Delay > 0f)
            {
                // PlayDelayed() can also be rescheduled
                AudioSource.PlayDelayed(_clip.Delay);
                _timeBeforeStartSchedule = _clip.Delay;
                isSchdeuledPlay = true;
            }

            if (_pref.ScheduledEndTime > 0d)
            {
                AudioSource.SetScheduledEndTime(_pref.ScheduledEndTime);
            }
        }

        IAudioPlayer ISchedulable.SetScheduledStartTime(double dspTime)
        {
            if(_pref.ScheduledStartTime > 0d)
            {
                _timeBeforeStartSchedule += (float)(dspTime - _pref.ScheduledStartTime);
            }
            _pref.ScheduledStartTime = dspTime;

            // isPlaying will return true once it's scheduled, even if it's not actually playing
            if (AudioSource.isPlaying)
            {
                // If this is called after the audio is already playing, it will pause until the given dspTime.
                // Some might consider this behavior a feature, so it has been left as is.
                AudioSource.SetScheduledStartTime(dspTime);
            }
            return this;
        }

        IAudioPlayer ISchedulable.SetScheduledEndTime(double dspTime)
        {
            _pref.ScheduledEndTime = dspTime;
            _onUpdate -= CheckScheduledEnd;
            _onUpdate += CheckScheduledEnd;

            if (AudioSource.isPlaying)
            {
                AudioSource.SetScheduledEndTime(dspTime);
            }
            return this;
        }

        private void CheckScheduledEnd(IAudioPlayer player)
        {
            if (!AudioSource.isPlaying)
            {
                this.SafeStopCoroutine(_playbackControlCoroutine);
                EndPlaying();
                _onUpdate -= CheckScheduledEnd;
            }
        }

        IAudioPlayer ISchedulable.SetDelay(float delay)
        {
            return this.SetScheduledStartTime(AudioSettings.dspTime + delay);
        }


        private IEnumerator WaitForScheduledStartTime()
        {
            while (_timeBeforeStartSchedule > 0)
            {
                yield return null;
                _timeBeforeStartSchedule -= Utility.GetDeltaTime();
            }
        }

        private void ClearScheduleEndEvents()
        {
            _onUpdate -= CheckScheduledEnd;
        }
    }
}
