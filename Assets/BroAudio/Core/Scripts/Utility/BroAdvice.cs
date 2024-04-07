using Ami.Extension;

namespace Ami.BroAudio
{
	public static class BroAdvice
	{
		public static float FullVolume => AudioConstant.FullVolume;

		public const float FadeTime_Immediate = 0f;
		public const float FadeTime_Quick = 0.5f;
		public const float FadeTime_Smooth = 1f;

		public const float LowPassFrequency = 300f;
		public const float HighPassFrequency = 2000f;

		public const Ease VolumeIncreaseEase = Ease.InCubic;
		public const Ease VolumeDecreaseEase = Ease.OutSine;

		public const Ease LowPassInEase = Ease.OutCubic;
		public const Ease LowPassOutEase = Ease.InCubic;
        public const Ease HighPassInEase = Ease.InCubic;
        public const Ease HighPassOutEase = Ease.OutCubic;

        public const int VirtualTrackCount = 4;
	}
}