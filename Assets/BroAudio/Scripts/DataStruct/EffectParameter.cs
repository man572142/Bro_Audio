using Ami.Extension;
using static Ami.BroAudio.Tools.BroLog;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio
{
	[System.Serializable]
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
				else if (Type == EffectType.HighCut || Type == EffectType.LowCut)
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

			switch (type)
			{
				case EffectType.Volume:
					Value = AudioConstant.FullVolume;
					break;
				case EffectType.HighCut:
					Value = BroAdvice.HighCutFrequence;
					break;
				case EffectType.LowCut:
					Value = BroAdvice.LowCutFrequence;
					break;
			}
		}

		public static class NameOf
		{
			public static string Value => nameof(_value);
		}
	}
}