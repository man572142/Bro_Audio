using Ami.Extension;
using static UnityEngine.Debug;

namespace Ami.BroAudio
{
	/// <summary>
	/// Parameters for setting effects. Please use the static factory methods within this class.
	/// </summary>
	[System.Serializable]
	public struct Effect
	{
		// Use these static methods for SetEffect()
		public static Effect HighPass(float frequency, float fadeTime = 0f, Ease ease = BroAdvice.HighPassInEase) 
			=> new Effect(EffectType.HighPass, frequency, SingleFading(fadeTime, ease));
        public static Effect ResetHighPass(float fadeTime = 0f, Ease ease = BroAdvice.HighPassOutEase) 
			=> new Effect(EffectType.HighPass, AudioConstant.MinFrequency, SingleFading(fadeTime, ease));
        public static Effect LowPass(float frequency, float fadeTime = 0f, Ease ease = BroAdvice.LowPassInEase) 
			=> new Effect(EffectType.LowPass, frequency, SingleFading(fadeTime, ease));
        public static Effect ResetLowPass(float fadeTime = 0f, Ease ease = BroAdvice.LowPassOutEase) 
			=> new Effect(EffectType.LowPass, AudioConstant.MaxFrequency, SingleFading(fadeTime, ease));
        public static Effect Custom(string exposedParameterName, float value, float fadeTime = 0f, Ease ease = Ease.Linear) 
			=> new Effect(exposedParameterName, value, SingleFading(fadeTime, ease));
        public static class Defaults
		{
			public static float Volume => AudioConstant.FullVolume;
			public static float LowPass => AudioConstant.MaxFrequency;
			public static float HighPass => AudioConstant.MinFrequency;
		}

		private static Fading SingleFading(float fadeTime, Ease ease) => new Fading(fadeTime, default, ease, default);


        private float _value;

		public readonly EffectType Type;
		public readonly Fading Fading;
		public readonly string CustomExposedParameter;
		internal readonly bool IsDominator;

		// Force user to use static factory method
		internal Effect(EffectType type, float value, Fading fading, bool isDominator = false) : this(type)
		{
			Value = value;
			Fading = fading;
			IsDominator = isDominator;
		}

		internal Effect(string exposedParaName, float value, Fading fading) : this(EffectType.Custom, value, fading)
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
						LogError(Utility.LogTitle + "EffectParameter's EffectType must be set before the Value");
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