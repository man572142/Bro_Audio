using Ami.Extension;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio
{
	/// <summary>
	/// Parameters for setting effects. Please use the static factory methods within this class.
	/// </summary>
	[System.Serializable]
	public struct Effect
	{
		// Use these static methods for SetEffect()
		public static Effect HighPass(float frequency, float fadeTime, Ease fadingEase = BroAdvice.HighPassInEase) => new Effect(EffectType.HighPass, frequency, fadeTime, fadingEase);
		public static Effect LowPass(float frequency, float fadeTime, Ease fadingEase = BroAdvice.LowPassInEase) => new Effect(EffectType.LowPass, frequency, fadeTime, fadingEase);
		public static Effect Custom(string exposedParameterName, float value, float fadeTime, Ease ease = Ease.Linear) => new Effect(exposedParameterName, value, fadeTime, ease);
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
		public readonly string CustomExposedParameter;
		internal readonly bool IsDominator;

		// Force user to use static factory method
		internal Effect(EffectType type, float value, float fadeTime, Ease fadingEase, bool isDominator = false) : this(type)
		{
			FadeTime = fadeTime;
			Value = value;
			FadingEase = fadingEase;
			IsDominator = isDominator;
		}

		internal Effect(string exposedParaName, float value, float fadeTime, Ease ease) : this(EffectType.Custom, value, fadeTime, ease)
		{
			CustomExposedParameter = exposedParaName;
		}

		public float Value
		{
			get => _value;
			private set
			{
				switch (Type)
				{
					case EffectType.None:
						LogError("EffectParameter's EffectType must be set before the Value");
						break;
					case EffectType.Volume:
						_value = value.ToDecibel();
						break;
					case EffectType.LowPass:
					case EffectType.HighPass:
						if (AudioExtension.IsValidFrequency(value))
						{
							_value = value;
						}
						break;
					default:
						_value = value; 
						break;
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

        public bool IsDefault()
        {
            switch (Type)
            {
                case EffectType.Volume:
                    return Value == AudioConstant.FullDecibelVolume;
                case EffectType.LowPass:
                    return Value == Defaults.LowPass;
                case EffectType.HighPass:
                    return Value == Defaults.HighPass;
            }
			return false;
        }
	}
}