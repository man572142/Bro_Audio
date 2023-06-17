namespace MiProduction.BroAudio
{
	public struct EffectParameter
	{
		public float Value;
		public float FadeTime;
		public EffectType Type;

		public EffectParameter(EffectType type) : this()
		{
			Type = type;

			Value = type switch
			{
				EffectType.Volume => 1f,
				EffectType.LowPass => BroRecommeneded.LowPassFrequence,
				EffectType.HighPass => BroRecommeneded.HighPassFrequence,
				_ => 0f,
			};
		}


	}
}