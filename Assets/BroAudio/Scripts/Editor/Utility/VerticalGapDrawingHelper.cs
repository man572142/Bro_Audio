using Ami.Extension;

namespace Ami.BroAudio.Editor
{
	public class VerticalGapDrawingHelper : IEditorDrawLineCounter
	{
		public float SingleLineSpace => 10f;
		public int DrawLineCount { get; set; }
		public float GetTotalSpace() => DrawLineCount * SingleLineSpace;

		public float GetSpace()
		{
			DrawLineCount++;
			return SingleLineSpace;
		}
	}
}