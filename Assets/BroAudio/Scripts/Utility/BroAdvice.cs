using Ami.Extension;

namespace Ami.BroAudio
{
	public static class BroAdvice
	{
		public const float FadeTime_Immediate = 0f;
		public const float FadeTime_Quick = 0.5f;
		public const float FadeTime_Smooth = 1f;

		public const float LowPassFrequence = 300f;
		public const float HighPassFrequence = 2000f;

		public const Ease LowPassEase = Ease.OutCubic;
		public const Ease HighPassEase = Ease.InCubic;
	}
}