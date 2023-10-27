﻿using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class TransportSerializedWrapper : ITransport
	{
		public const int FloatFieldDigits = 2;

		private readonly SerializedProperty DelayProp;
		private readonly SerializedProperty StartPosProp;
		private readonly SerializedProperty EndPosProp;
		private readonly SerializedProperty FadeInProp;
		private readonly SerializedProperty FadeOutProp;

		public TransportSerializedWrapper(SerializedProperty startPosProp, SerializedProperty endPosProp, SerializedProperty fadeInProp, SerializedProperty fadeOutProp, SerializedProperty delayProp,float fullLength)
		{
			DelayProp = delayProp;
			StartPosProp = startPosProp;
			EndPosProp = endPosProp;
			FadeInProp = fadeInProp;
			FadeOutProp = fadeOutProp;
			Length = fullLength;

			PlaybackValues = new float[] { StartPosition, EndPosition, Delay };
			FadingValues = new float[] { FadeIn, FadeOut };
		}
		public float Delay => DelayProp.floatValue;
		public float StartPosition => StartPosProp.floatValue;
		public float EndPosition => EndPosProp.floatValue;
		public float FadeIn => FadeInProp.floatValue;
		public float FadeOut => FadeOutProp.floatValue;
		public float Length { get; set; }
		public float[] PlaybackValues { get; private set; }
		public float[] FadingValues { get; private set; }

		public void SetValue(float newValue, TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.Start:
					PlaybackValues[0] = ClampAndRound(newValue, StartPosProp);
					StartPosProp.floatValue = PlaybackValues[0];
					break;
				case TransportType.End:
					PlaybackValues[1] = ClampAndRound(newValue, EndPosProp);
					EndPosProp.floatValue = PlaybackValues[1];
					break;
				case TransportType.Delay:
					PlaybackValues[2] = Mathf.Max(newValue, 0f);
					DelayProp.floatValue = PlaybackValues[2];
					break;
				case TransportType.FadeIn:
					FadingValues[0] = ClampAndRound(newValue, FadeInProp);
					FadeInProp.floatValue = FadingValues[0];
					break;
				case TransportType.FadeOut:
					FadingValues[1] = ClampAndRound(newValue, FadeOutProp);
					FadeOutProp.floatValue = FadingValues[1];
					break;
			}
		}

		public void Update()
		{
			PlaybackValues[0] = StartPosition;
			PlaybackValues[1] = EndPosition;
			PlaybackValues[2] = Delay;
			FadingValues[0] = FadeIn;
			FadingValues[1] = FadeOut;
		}

		private float ClampAndRound(float value,SerializedProperty prop)
		{
			float clamped = Mathf.Clamp(value, 0f, GetLengthLimit(prop));
			return (float)System.Math.Round(clamped, FloatFieldDigits); 
		}

		private float GetLengthLimit(SerializedProperty modifiedProp)
		{
			return Length - GetOccupyLength(StartPosProp) - GetOccupyLength(EndPosProp) - GetOccupyLength(FadeInProp) - GetOccupyLength(FadeOutProp);

			float GetOccupyLength(SerializedProperty property)
			{
				if(property != modifiedProp)
				{
					return property.floatValue;
				}
				return 0f;
			}
		}
	}
}
