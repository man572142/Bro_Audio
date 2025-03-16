using UnityEngine.Audio;

namespace Ami.BroAudio.Runtime
{
	public interface IAudioMixer
	{
        AudioMixer AudioMixer { get; }
        internal AudioMixerGroup GetTrack(AudioTrackType trackType);
		internal void ReturnTrack(AudioTrackType trackType, AudioMixerGroup track);
        internal void ReturnPlayer(AudioPlayer player);
    }
}
