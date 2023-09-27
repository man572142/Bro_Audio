using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public struct TransportSerializeWrapper : ITransport
	{
		private readonly SerializedProperty StartPosProp;
		private readonly SerializedProperty EndPosProp;
		private readonly SerializedProperty FadeInProp;
		private readonly SerializedProperty FadeOutProp;

		private float[] _playbackValues;
		private float[] _fadeValues;

		public TransportSerializeWrapper(SerializedProperty startPosProp, SerializedProperty endPosProp, SerializedProperty fadeInProp, SerializedProperty fadeOutProp, float fullLength) : this()
		{
			StartPosProp = startPosProp;
			EndPosProp = endPosProp;
			CreateMultiFloat(ref _playbackValues, startPosProp, endPosProp);
			FadeInProp = fadeInProp;
			FadeOutProp = fadeOutProp;
			CreateMultiFloat(ref _fadeValues, fadeInProp, fadeOutProp);
			FullLength = fullLength;
		}

		public float StartPosition { get => StartPosProp.floatValue; set => ClampAndSetValue(ref _playbackValues[0],value,StartPosProp); }
		public float EndPosition { get => EndPosProp.floatValue; set => ClampAndSetValue(ref _playbackValues[1], value, EndPosProp); }
		public float FadeIn { get => FadeInProp.floatValue; set => ClampAndSetValue(ref _fadeValues[0], value, FadeInProp); }
		public float FadeOut { get => FadeOutProp.floatValue; set => ClampAndSetValue(ref _fadeValues[1], value, FadeOutProp); }
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
			}
			return null;
		}


		public void ClampAndSetProperty(TransportType transportType)
		{
			switch (transportType)
			{
				case TransportType.PlaybackPosition:
					ClampAndSetProperty(ref _playbackValues[0], StartPosProp);
					ClampAndSetProperty(ref _playbackValues[1], EndPosProp);
					break;
				case TransportType.Fading:
					ClampAndSetProperty(ref _fadeValues[0], FadeInProp);
					ClampAndSetProperty(ref _fadeValues[1], FadeOutProp);
					break;
			}
		}

		private void ClampAndSetProperty(ref float value, SerializedProperty prop)
		{
			value = Mathf.Clamp(value, 0f, GetLengthLimit(prop));
			value = (float)System.Math.Round(value, DrawClipPropertiesHelper.FloatFieldDigits);
			prop.floatValue = value;
		}

		private void ClampAndSetValue(ref float target,float newValue, SerializedProperty prop)
		{
			target = Mathf.Clamp(newValue, 0f, GetLengthLimit(prop));
			target = (float)System.Math.Round(target, DrawClipPropertiesHelper.FloatFieldDigits);
			prop.floatValue = target;
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
