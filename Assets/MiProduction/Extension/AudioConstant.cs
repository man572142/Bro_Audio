namespace MiProduction.Extension
{
	public static class AudioConstant
	{
		/// <summary>
		/// The minimum volume that Unity AudioMixer can reach in dB.
		/// </summary>
		public const float MinDecibelVolume = -80f;
		/// <summary>
		/// The full (or default) volume in dB.
		/// </summary>
		public const float FullDecibelVolume = 0f;
		/// <summary>
		/// The maximum volume that Unity AudioMixer can reach in dB.
		/// </summary>
		public const float MaxDecibelVolume = 20f;


		/// <summary>
		/// The normalized minimum volume that Unity AudioMixer can reach
		/// </summary>
		public const float MinVolume = 0.0001f;

		/// <summary>
		/// The normalized full (or default) volume
		/// </summary>
		public const float FullVolume = 1f;

		/// <summary>
		/// The normalized maximum volume that Unity AudioMixer can reach
		/// </summary>
		public const float MaxVolume = 10f;


		/// <summary>
		/// The maximum sound frequence in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
		/// </summary>
		public const float MaxFrequence = 22000f;

		/// <summary>
		/// The minimum sound frequence in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
		/// </summary>
		public const float MinFrequence = 10f;

		public static float DecibelVoulumeFullScale => MaxDecibelVolume - MinDecibelVolume;
	}

}