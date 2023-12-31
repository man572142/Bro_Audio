using Ami.Extension;
using static Ami.BroAudio.Tools.BroLog;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio
{
	/// <summary>
	/// Parameters for setting effects. Please use the static factory methods within this class.
	/// </summary>
	[System.Serializable]
	public struct Effect
	{
		// Use these static methods for SetEffect() is recomended
		public static Effect HighPass(float frequency, float fadeTime, Ease fadingEase = BroAdvice.HighPassEase) => new Effect(EffectType.HighPass, frequency, fadeTime, fadingEase);
		public static Effect LowPass(float frequency, float fadeTime, Ease fadingEase = BroAdvice.LowPassEase) => new Effect(EffectType.LowPass, frequency, fadeTime, fadingEase);
		public static Effect Volume(float volumeFactor, float fadeTime, Ease fadingEase = default) => new Effect(EffectType.Volume, volumeFactor, fadeTime, fadingEase);

		public static class Defaults
		{
			public static float Volume => AudioConstant.FullVolume;
			public static float LowPass => AudioConstant.MaxFrequency;
			public static float HighPass => AudioConstant.MinFrequency;
		}
		

		private float _value;

		public readonly EffectType Type;
		public readonly float FadeTime;
		public readonly Ease FadingEase;

		internal Effect(EffectType type, float value, float fadeTime, Ease fadingEase) : this(type)
		{
			FadeTime = fadeTime;
			Value = value;
			FadingEase = fadingEase;
		}

		public float Value
		{
			get => _value;
			private set
			{
				if(Type == EffectType.None)
				{
					LogError("EffectParameter's EffectType must be set before the Value");
					return;
				}

				if(Type == EffectType.Volume)
				{
					_value = value.ToDecibel();
				}
				else if (Type == EffectType.LowPass || Type == EffectType.HighPass)
				{
					if(AudioExtension.IsValidFrequency(value))
					{
						_value = value;
					}
				}
			}
		}

		public Effect(EffectType type) : this()
		{
			Type = type;

			switch (type)
			{
				case EffectType.Volume:
					Value = AudioConstant.FullVolume;
					break;
				case EffectType.LowPass:
					Value = BroAdvice.LowPassFrequency;
					break;
				case EffectType.HighPass:
					Value = BroAdvice.HighPassFrequency;
					break;
			}
		}
	}
}