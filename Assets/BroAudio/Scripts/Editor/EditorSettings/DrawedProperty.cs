using System;

namespace Ami.BroAudio.Editor
{
	[Flags]
	public enum DrawedProperty
	{
		// Basic
		Volume,
		PlaybackPosition,
		Fade,
		ClipPreview,

		// Additional
		Delay,
		Loop,
		SeamlessLoop,
		Priority,
	} 
}