namespace MiProduction.BroAudio.ClipEditor 
{
	public struct Transport
	{
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		public float FullLength;

		public bool HasDifferentPosition => StartPosition != 0f || EndPosition != 0f;
		public bool HasFading => FadeIn != 0f || FadeOut != 0f;
	}
}