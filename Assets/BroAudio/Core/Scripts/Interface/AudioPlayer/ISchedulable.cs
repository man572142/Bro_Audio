namespace Ami.BroAudio
{
    public interface ISchedulable
    {
        /// <summary>
        /// Schedules the clip to start playing at a specific time on the absolute timeline that AudioSettings.dspTime reads from .
        /// </summary>
        /// <param name="dspTime">The absolute time in seconds when the clip should start playing.</param>
        /// <remarks>If the clip hasn't played yet, this is equivalent to AudioSource.PlayScheduled, otherwise, it reschedules the time</remarks>
        IAudioPlayer SetScheduledStartTime(double dspTime);

        ///<inheritdoc cref="UnityEngine.AudioSource.SetScheduledEndTime(double)(double)"/>
        IAudioPlayer SetScheduledEndTime(double dspTime);

        /// <summary>
        /// Delays the playback start time by the specified duration in seconds.
        /// </summary>
        /// <param name="time">The delay duration in seconds.</param>
        IAudioPlayer SetDelay(float time);
    }
}
