using UnityEngine.Audio;

namespace Ami.BroAudio.Runtime
{
	public interface IAudioMixer
	{
#if !UNITY_WEBGL
        AudioMixer Mixer { get; }
#endif
#if UNITY_2020_2_OR_NEWER
        internal AudioMixerGroup GetTrack(AudioTrackType trackType);
		internal void ReturnTrack(AudioTrackType trackType, AudioMixerGroup track);
#else
		AudioMixerGroup GetTrack(AudioTrackType trackType);
		void ReturnTrack(AudioTrackType trackType, AudioMixerGroup track);
#endif
    }
}
