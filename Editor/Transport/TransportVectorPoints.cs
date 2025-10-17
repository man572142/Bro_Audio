using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public struct TransportVectorPoints
	{
		public readonly ITransport Transport;
		public readonly Vector2 DrawingSize;
		public readonly float ClipLength;

		public TransportVectorPoints(ITransport transport, Vector2 drawingSize, float clipLength)
		{
			Transport = transport;
			DrawingSize = drawingSize;
			ClipLength = clipLength;
		}

		public Vector3 Start => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (Transport.StartPosition + GetExceededTime()) / ClipLength), DrawingSize.y);
		public Vector3 FadeIn => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (Transport.StartPosition + Transport.FadeIn + GetExceededTime()) / ClipLength), 0f);
		public Vector3 FadeOut => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition - Transport.FadeOut) / ClipLength), 0f);
		public Vector3 End => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition) / ClipLength), DrawingSize.y);
		public Vector3[] GetVectorsClockwise()
		{
			return new Vector3[] { Start, FadeIn, FadeOut, End };
		}

		public float GetExceededTime()
		{
			return Mathf.Max(0f, Transport.Delay - Transport.StartPosition);
		}
	}
}
