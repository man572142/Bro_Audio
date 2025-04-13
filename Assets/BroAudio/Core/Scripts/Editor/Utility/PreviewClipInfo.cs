using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public struct PreviewClipInfo : IClip
	{
		public float StartPosition { get; set; }
		public float EndPosition { get; set; }
		public float FullLength { get; set; }

		public PreviewClipInfo(IClip clip)
		{
			StartPosition = clip.StartPosition;
			EndPosition = clip.EndPosition;
			FullLength = clip.FullLength;
		}

		public PreviewClipInfo(IBroAudioClip clip)
		{
            StartPosition = clip.StartPosition;
            EndPosition = clip.EndPosition;
            FullLength = clip.GetAudioClip().length;
        }

		public PreviewClipInfo(float length) : this()
		{
			FullLength = length;
		}
	} 
}