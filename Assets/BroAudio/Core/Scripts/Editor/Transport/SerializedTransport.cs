using Ami.BroAudio.Data;
using System;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class SerializedTransport : Transport
	{
		private readonly SerializedProperty DelayProp;
		public readonly SerializedProperty StartPosProp;
		private readonly SerializedProperty EndPosProp;
		private readonly SerializedProperty FadeInProp;
		private readonly SerializedProperty FadeOutProp;

		public SerializedTransport(SerializedProperty clipProp,float fullLength) : base(fullLength)
		{
            DelayProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Delay));
            StartPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
            EndPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
            FadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
            FadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));
		}
		public override float Delay => DelayProp.floatValue;
		public override float StartPosition => StartPosProp.floatValue;
		public override float EndPosition => EndPosProp.floatValue;
		public override float FadeIn => FadeInProp.floatValue;
		public override float FadeOut => FadeOutProp.floatValue;

		public override void SetValue(float newValue, TransportType transportType)
		{
			base.SetValue(newValue, transportType);

			switch (transportType)
			{
				case TransportType.Start:
					StartPosProp.floatValue = PlaybackValues[0];
					break;
				case TransportType.End:
					EndPosProp.floatValue = PlaybackValues[1];
					break;
				case TransportType.Delay:
					DelayProp.floatValue = PlaybackValues[2];
					break;
				case TransportType.FadeIn:
					FadeInProp.floatValue = FadingValues[0];
					break;
				case TransportType.FadeOut:
					FadeOutProp.floatValue = FadingValues[1];
					break;
			}
			StartPosProp.serializedObject.ApplyModifiedProperties();
		}
	}
}
