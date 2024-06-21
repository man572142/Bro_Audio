using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
	public class AudioTypePlaybackPreference : IAudioPlaybackPref
	{
		// Each audio type will only have one instance
		public float Volume { get; set; } = AudioConstant.FullVolume;
		public float Pitch { get; set; } = AudioConstant.DefaultPitch;
		public EffectType EffectType { get; set; }
	}

	public interface IAudioPlaybackPref
	{
		float Volume { get; }
        float Pitch { get; }
        EffectType EffectType { get; }
	}
}

