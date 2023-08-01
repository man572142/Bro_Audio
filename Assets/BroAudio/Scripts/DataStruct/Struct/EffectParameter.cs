using Ami.Extension;
using static Ami.BroAudio.BroLog;

namespace Ami.BroAudio
{
	public struct EffectParameter
	{
		private float _value;

		public EffectType Type;
		public float FadeTime;
		public Ease FadingEase;

		public float Value
		{
			get => _value;
			set
			{
				if(Type == EffectType.None)
				{
					LogError("EffectParameter's EffectType must be set before the Value");
					return;
				}

				if(Type == EffectType.Volume)
				{
					if (value <= 1f && value >= 0f)
					{
						_value = value.ToDecibel();						
					}
					else
					{
						LogWarning("The value of a volume type EffectParameter should be less than 1 and greater than 0!");
					}
				}
				else if (Type == EffectType.LowPass || Type == EffectType.HighPass)
				{
					if(AudioExtension.IsValidFrequence(value))
					{
						_value = value;
					}
				}
			}
		}

		public EffectParameter(EffectType type) : this()
		{
			Type = type;

			Value = type switch
			{
				EffectType.Volume => AudioConstant.FullVolume,
				EffectType.LowPass => BroAdvice.LowPassFrequence,
				EffectType.HighPass => BroAdvice.HighPassFrequence,
				_ => 0f,
			};
		}
	}
}