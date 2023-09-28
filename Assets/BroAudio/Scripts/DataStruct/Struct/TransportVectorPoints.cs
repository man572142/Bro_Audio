﻿using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public struct TransportVectorPoints
	{
		public readonly IReadOnlyTransport Transport;
		public readonly Vector2 DrawingSize;
		public readonly float ClipLength;

		public TransportVectorPoints(IReadOnlyTransport transport, Vector2 drawingSize, float clipLength)
		{
			Transport = transport;
			DrawingSize = drawingSize;
			ClipLength = clipLength;
		}

		public Vector3 Start => new Vector3(Mathf.Lerp(0f, DrawingSize.x, Transport.StartPosition / ClipLength), DrawingSize.y);
		public Vector3 FadeIn => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (Transport.StartPosition + Transport.FadeIn) / ClipLength), 0f);
		public Vector3 FadeOut => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition - Transport.FadeOut) / ClipLength), 0f);
		public Vector3 End => new Vector3(Mathf.Lerp(0f, DrawingSize.x, (ClipLength - Transport.EndPosition) / ClipLength), DrawingSize.y);
		public Vector3[] GetVectorsClockwise()
		{
			return new Vector3[] { Start, FadeIn, FadeOut, End };
		}
	}
}
