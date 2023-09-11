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

		// Additional (start from 10)
		Delay = 1 << 10,
		Loop = 1 << 11,
		Priority = 1 << 12,
	} 

	public static class DrawedPropertyConstant
	{
		public const int AdditionalPropertyStartIndex = 10;
	}
}