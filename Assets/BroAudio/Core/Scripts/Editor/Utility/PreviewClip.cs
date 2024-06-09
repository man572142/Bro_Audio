using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public struct PreviewClip : IClip
	{
		public float StartPosition { get; set; }
		public float EndPosition { get; set; }
		public float FullLength { get; set; }

		public PreviewClip(IClip clip)
		{
			StartPosition = clip.StartPosition;
			EndPosition = clip.EndPosition;
			FullLength = clip.FullLength;
		}

		public PreviewClip(BroAudioClip clip)
		{
            StartPosition = clip.StartPosition;
            EndPosition = clip.EndPosition;
            FullLength = clip.AudioClip.length;
        }

		public PreviewClip(float length) : this()
		{
			FullLength = length;
		}
	} 
}