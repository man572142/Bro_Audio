using System;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor 
{
	public class Transport : ITransport, IReadOnlyTransport
	{
		public event Action<TransportType> OnTransportChanged;
		public float StartPosition { get; set; }
		public float EndPosition { get; set; }
		public float FadeIn { get; set; }
		public float FadeOut { get; set; }
		public float FullLength { get; set; }

		public Transport(BroAudioClip clip)
		{
			StartPosition = clip.StartPosition;
			EndPosition = clip.EndPosition;
			FadeIn = clip.FadeIn;
			FadeOut = clip.FadeOut;
            FullLength = 0f;
            if (clip.AudioClip)
			{
                FullLength = clip.AudioClip.length;
            }
		}

		public bool HasDifferentPosition => StartPosition != 0f || EndPosition != 0f;
		public bool HasFading => FadeIn != 0f || FadeOut != 0f;

		public float[] GetMultiFloatValues(TransportType transportType)
		{
			//switch (transportType)
			//{
			//	case TransportType.PlaybackPosition:
			//		return GetOrCreateValues();
			//		break;
			//	case TransportType.Fading:
			//		break;
			//}
			return null;
		}

		public void ClampAndSetProperty(TransportType transportType)
		{
			throw new System.NotImplementedException();
		}

		private float[] GetOrCreateValues(float[] values, params float[] sources)
		{
			if(values == null)
			{
				values = new float[sources.Length];

				for (int i = 0; i < sources.Length; i++)
				{
					values[i] = sources[i];
				}
			}
			return values;
		}
	}
}