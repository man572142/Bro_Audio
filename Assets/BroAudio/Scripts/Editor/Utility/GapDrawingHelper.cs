using Ami.Extension;

namespace Ami.Extension
{
	public class GapDrawingHelper : IEditorDrawLineCounter
	{
		public float SingleLineSpace => 10f;
		public int DrawLineCount { get; set; }
        public float Offset { get; set; }
        public float GetTotalSpace() => DrawLineCount * SingleLineSpace;

		public float GetSpace()
		{
			DrawLineCount++;
			return SingleLineSpace;
		}
	}
}