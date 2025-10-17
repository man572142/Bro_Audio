using UnityEngine;

namespace Ami.Extension
{
	public class EditorScrollViewHelper
	{
		private IEditorDrawLineCounter _lineCounter;
		private Rect _scrollViewRect = default;

		private int _endLine = default;

		public EditorScrollViewHelper(IEditorDrawLineCounter lineCounter)
		{
			_lineCounter = lineCounter;
		}

		public Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical)
		{
			float height = (_endLine - _lineCounter.DrawLineCount) * _lineCounter.SingleLineSpace;
			_scrollViewRect = new Rect(position.x, position.y, position.width, Mathf.Max(0f,height));
			position.width += 15f; // extra space for vertical scroll bar
			return GUI.BeginScrollView(position, scrollPosition, _scrollViewRect,alwaysShowHorizontal,alwaysShowVertical);
		}

		public void EndScrollView()
		{
			_endLine = _lineCounter.DrawLineCount;
			GUI.EndScrollView();
		}
	}
}