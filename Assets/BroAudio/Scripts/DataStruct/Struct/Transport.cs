using System;
using UnityEngine;

namespace Ami.BroAudio.Editor 
{
	public class Transport : ITransport
	{
		public const int FloatFieldDigits = 2;

		public float StartPosition { get; set; }
		public float EndPosition { get; set; }
		public float FadeIn { get; set; }
		public float FadeOut { get; set; }
		public float FullLength { get; set; }
		public float[] PlaybackValues { get; private set; }
		public float[] FadingValues { get; private set; }

		public Transport(AudioClip clip)
		{
            if (clip)
			{
                FullLength = clip.length;
            }
			PlaybackValues = new float[] { StartPosition, EndPosition };
			FadingValues = new float[] { FadeIn, FadeOut };
		}

		public bool HasDifferentPosition => StartPosition != 0f || EndPosition != 0f;
		public bool HasFading => FadeIn != 0f || FadeOut != 0f;

		public void SetValue(float newValue, TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.Start:
					PlaybackValues[0] = ClampAndRound(newValue, StartPosition);
					StartPosition = PlaybackValues[0];
					break;
				case TransportType.End:
					PlaybackValues[1] = ClampAndRound(newValue, EndPosition);
					EndPosition = PlaybackValues[1];
					break;
				case TransportType.FadeIn:
					FadingValues[0] = ClampAndRound(newValue, FadeIn);
					FadeIn = FadingValues[0];
					break;
				case TransportType.FadeOut:
					FadingValues[1] = ClampAndRound(newValue, FadeOut);
					FadeOut = FadingValues[1];
					break;
			}
		}

		private float ClampAndRound(float value, float targetValue)
		{
			float clamped = Mathf.Clamp(value, 0f, GetLengthLimit(targetValue));
			return (float)System.Math.Round(clamped, FloatFieldDigits);
		}

		private float GetLengthLimit(float targetValue)
		{
			return FullLength - StartPosition - FadeIn - FadeOut - EndPosition + targetValue;
		}
	}
}