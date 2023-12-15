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
		// Use these static method for SetEffect() is recomended
		public static Effect LowCut(float frequence, float fadeTime, Ease fadingEase = default) => new Effect(EffectType.LowCut, frequence, fadeTime, fadingEase);
		public static Effect HighCut(float frequence, float fadeTime, Ease fadingEase = default) => new Effect(EffectType.HighCut, frequence, fadeTime, fadingEase);
		public static Effect Volume(float volumeFactor, float fadeTime, Ease fadingEase = default) => new Effect(EffectType.Volume, volumeFactor, fadeTime, fadingEase);

		public static class Defaults
		{
			public static float Volume => AudioConstant.FullVolume;
			public static float HighCut => AudioConstant.MaxFrequence;
			public static float LowCut => AudioConstant.MinFrequence;
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

		public Effect(EffectType type) : this()
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
	}
}