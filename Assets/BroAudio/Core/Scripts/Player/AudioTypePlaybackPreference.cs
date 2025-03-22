using System;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
	public class AudioTypePlaybackPreference : IAudioPlaybackPref
	{
        public struct SetEffectParameter
        {
            public EffectType EffectType;
            public SetEffectMode Mode;
        }

        public static readonly Action<AudioTypePlaybackPreference, float> OnSetVolume = SetVolume;
        public static readonly Action<AudioTypePlaybackPreference, float> OnSetpitch = SetPitch;
        public static readonly Action<AudioTypePlaybackPreference, SetEffectParameter> OnSetEffect = SetEffect;

        public float Volume { get; private set; } = AudioConstant.FullVolume;
		public float Pitch { get; private set; } = AudioConstant.DefaultPitch;
		public EffectType EffectType { get; private set; }

		private static void SetVolume(AudioTypePlaybackPreference pref, float vol)
		{
            pref.Volume = vol;
		}

        private static void SetPitch(AudioTypePlaybackPreference pref, float pitch)
        {
            pref.Pitch = pitch;
        }

        private static void SetEffect(AudioTypePlaybackPreference pref, SetEffectParameter parameter)
        {
            switch (parameter.Mode)
            {
                case SetEffectMode.Add:
                    pref.EffectType |= parameter.EffectType;
                    break;
                case SetEffectMode.Remove:
                    pref.EffectType &= ~parameter.EffectType;
                    break;
                case SetEffectMode.Override:
                    pref.EffectType = parameter.EffectType;
                    break;
            }
        }
    }

	public interface IAudioPlaybackPref
	{
		float Volume { get; }
        float Pitch { get; }
        EffectType EffectType { get; }
	}
}

