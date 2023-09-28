using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class TransportSerializedWrapper : ITransport, IReadOnlyTransport
	{
		public event Action<TransportType> OnTransportChanged;

		private readonly SerializedProperty StartPosProp;
		private readonly SerializedProperty EndPosProp;
		private readonly SerializedProperty FadeInProp;
		private readonly SerializedProperty FadeOutProp;

		private readonly float[] _playbackValues;
		private readonly float[] _fadeValues;

		public TransportSerializedWrapper(SerializedProperty startPosProp, SerializedProperty endPosProp, SerializedProperty fadeInProp, SerializedProperty fadeOutProp, float fullLength)
		{
			StartPosProp = startPosProp;
			EndPosProp = endPosProp;
			CreateMultiFloat(ref _playbackValues, startPosProp, endPosProp);
			FadeInProp = fadeInProp;
			FadeOutProp = fadeOutProp;
			CreateMultiFloat(ref _fadeValues, fadeInProp, fadeOutProp);
			FullLength = fullLength;
		}

		public float StartPosition { get => StartPosProp.floatValue; set => SetValue(value,TransportType.Start); }
		public float EndPosition { get => EndPosProp.floatValue; set => SetValue(value, TransportType.End); }
		public float FadeIn { get => FadeInProp.floatValue; set => SetValue(value, TransportType.FadeIn); }
		public float FadeOut { get => FadeOutProp.floatValue; set => SetValue(value, TransportType.FadeOut); }
		public float FullLength { get; set; }

		private void CreateMultiFloat(ref float[] values, params SerializedProperty[] properites)
		{
			if (values == null)
			{
				values = new float[properites.Length];
				for (int i = 0; i < properites.Length; i++)
				{
					values[i] = properites[i].floatValue;
				}
			}
		}

		public float[] GetMultiFloatValues(TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.PlaybackPosition:
					return _playbackValues;
				case TransportType.Fading:
					return _fadeValues;
				default:
					Tools.BroLog.LogError($"{transportType} is not multiFloat type");
					return null;
			}
		}

		private void SetValue(float newValue, TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.Start:
					_playbackValues[0] = ClampAndRound(newValue, StartPosProp);
					break;
				case TransportType.End:
					_playbackValues[1] = ClampAndRound(newValue, EndPosProp);
					break;
				case TransportType.FadeIn:
					_fadeValues[0] = ClampAndRound(newValue, FadeInProp);
					break;
				case TransportType.FadeOut:
					_fadeValues[1] = ClampAndRound(newValue, FadeOutProp);
					break;
				case TransportType.PlaybackPosition:
					_playbackValues[0] = ClampAndRound(newValue, StartPosProp);
					_playbackValues[1] = ClampAndRound(newValue, EndPosProp);
					break;
				case TransportType.Fading:
					_fadeValues[0] = ClampAndRound(newValue, FadeInProp);
					_fadeValues[1] = ClampAndRound(newValue, FadeOutProp);
					break;
				default:
					Tools.BroLog.LogError($"No corresponding serializedProperty can be set by transport type {transportType}");
					return;
			}
			SetPropertyByMultiFloatValues(transportType);
			OnTransportChanged?.Invoke(transportType);
		}

		private void SetPropertyByMultiFloatValues(TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.Start:
					StartPosProp.floatValue = _playbackValues[0];
					break;
				case TransportType.End:
					EndPosProp.floatValue = _playbackValues[1];
					break;
				case TransportType.FadeIn:
					FadeInProp.floatValue = _fadeValues[0];
					break;
				case TransportType.FadeOut:
					FadeOutProp.floatValue = _fadeValues[1];
					break;
				case TransportType.PlaybackPosition:
					StartPosProp.floatValue = _playbackValues[0];
					EndPosProp.floatValue = _playbackValues[1];
					break;
				case TransportType.Fading:
					FadeInProp.floatValue = _fadeValues[0];
					FadeOutProp.floatValue = _fadeValues[1];
					break;
			}
		}

		private float ClampAndRound(float value,SerializedProperty prop)
		{
			float clamped = Mathf.Clamp(value, 0f, GetLengthLimit(prop));
			return (float)System.Math.Round(clamped, DrawClipPropertiesHelper.FloatFieldDigits); 
		}

		private float GetLengthLimit(SerializedProperty modifiedProp)
		{
			return FullLength - GetOccupyLength(StartPosProp) - GetOccupyLength(EndPosProp) - GetOccupyLength(FadeInProp) - GetOccupyLength(FadeOutProp);

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
