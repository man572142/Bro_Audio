using UnityEngine.Audio;

namespace Ami.BroAudio.Runtime
{
	public interface IAudioMixer
	{
#if !UNITY_WEBGL
        AudioMixer Mixer { get; }
		internal AudioMixerGroup GetTrack(AudioTrackType trackType);
        internal void ReturnTrack(AudioTrackType trackType, AudioMixerGroup track); 
#endif
	} 
}