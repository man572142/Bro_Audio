using Ami.Extension;

namespace Ami.BroAudio.Tools
{
	public static class BroAdvice
	{
		public const float FadeTime_Immediate = 0f;
		public const float FadeTime_Quick = 0.5f;
		public const float FadeTime_Smooth = 1f;

		public const float HighCutFrequence = 300f;
		public const float LowCutFrequence = 2000f;

		public const Ease HighCutEase = Ease.OutCubic;
		public const Ease LowCutEase = Ease.InCubic;

		public const int VirtualTrackCount = 8;
	}
}