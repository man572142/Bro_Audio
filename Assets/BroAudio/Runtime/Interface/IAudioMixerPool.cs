using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.BroAudio.Runtime
{
    public interface IAudioMixerPool
    {
        internal AudioMixerGroup GetTrack(AudioTrackType trackType);
        internal void ReturnTrack(AudioTrackType trackType, AudioMixerGroup track);
        internal void ReturnPlayer(AudioPlayer player);
        
        /// <summary>
        /// This workaround resolves an old Unity issue where AudioMixer.SetFloat() doesn't work in the first Awake() and OnEnable() of the game. See https://discussions.unity.com/t/audiomixer-setfloat-doesnt-work-on-awake/579429/57 
        /// </summary>
        internal WaitForEndOfFrame WaitForAudioMixerInitialization { get; }
    } 
}