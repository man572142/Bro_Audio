using UnityEngine;
using Ami.Extension;
using System.Collections;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        private double _secondsUntilScheduledStart;
        private double _pauseDspTime;
        private double _playbackEndDspTime;

        private void SchedulePlayback()
        {
            if (_pref.ScheduledStartTime > 0d) // Scheduled has higher priority than clip.delay
            {
                var dspTime = AudioSettings.dspTime;
                AudioSource.PlayScheduled(System.Math.Max(_pref.ScheduledStartTime, dspTime));
                _secondsUntilScheduledStart = _pref.ScheduledStartTime - dspTime;
            }

            ScheduleEndTime();
        }

        private void ScheduleEndTime()
        {
            if (_pref.ScheduledEndTime > 0d)
            {
                AudioSource.SetScheduledEndTime(_pref.ScheduledEndTime);
            }
        }

        // Slide dsp-time schedule forward by the pause duration; caller re-arms the end via ScheduleEndTime after UnPause (PlayScheduled would restart the source).
        private void RebaseScheduleAfterPause()
        {
            double pauseDuration = AudioSettings.dspTime - _pauseDspTime;
            if (_pref.ScheduledStartTime > 0)
            {
                _pref.ScheduledStartTime += pauseDuration;
            }
            if (_pref.ScheduledEndTime > 0)
            {
                _pref.ScheduledEndTime += pauseDuration;
            }
        }

        IAudioPlayer ISchedulable.SetScheduledStartTime(double dspTime)
        {
            if (_pref.ScheduledStartTime > 0d)
            {
                // Recalculate the time when WaitForScheduledStartTime() is already running
                _secondsUntilScheduledStart += dspTime - _pref.ScheduledStartTime;
            }
            _pref.ScheduledStartTime = dspTime;

            // isPlaying will return true once it's scheduled, even if it's not actually playing
            if (AudioSource.isPlaying)
            {
                // If this is called after the audio is already playing, it will pause until the given dspTime.
                // Some might consider this behavior a feature, so it has been left as is.
                AudioSource.SetScheduledStartTime(dspTime);
            }
            else
            {
                PlayInternal();
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

        private void SetClipDelayIfNotScheduled()
        {
            if (_pref.ScheduledStartTime <= 0 && _clip != null && _clip.Delay > 0)
            {
                _pref.ScheduledStartTime = AudioSettings.dspTime + _clip.Delay;
            }
        }

        private IEnumerator WaitForScheduledStartTime()
        {
            while (_secondsUntilScheduledStart > 0)
            {
                yield return null;
                _secondsUntilScheduledStart -= Utility.GetDeltaTime();
            }
        }

        private void ClearScheduleEndEvents()
        {
            _onUpdate -= CheckScheduledEnd;
        }
    }
}