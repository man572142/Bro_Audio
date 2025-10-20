namespace Ami.Extension
{
	public interface IEditorDrawLineCounter
	{
		float SingleLineSpace { get; }
		int DrawLineCount { get; set; }
		float Offset { get; set; }
	}
}