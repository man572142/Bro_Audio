using System;

namespace Ami.BroAudio.Editor
{
	[Flags]
	public enum DrawedProperty
	{
		// Basic
		Volume = 1 << 0,
		PlaybackPosition = 1 << 1,
		Fade = 1 << 2,
		ClipPreview = 1 << 3,

		// Additional
		Delay = 1 << 4,
		Loop = 1 << 5,
		SeamlessLoop = 1 << 6,
		Priority = 1 << 7,
	} 
}