using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
	public class AudioTypePlaybackPreference : IAudioPlaybackPref
	{
		// Each audio type will only have one instance
		public float Volume { get; set; } = AudioConstant.FullVolume;
		public EffectType EffectType { get; set; }
	}

	public interface IAudioPlaybackPref
	{
		public float Volume { get; }
		public EffectType EffectType { get; }
	}
}

