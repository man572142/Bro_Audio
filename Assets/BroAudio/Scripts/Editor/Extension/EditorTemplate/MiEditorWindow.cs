using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.Extension
{
	public abstract class MiEditorWindow : EditorWindow, IEditorDrawLineCounter
	{
		public abstract float SingleLineSpace { get; }
		public int DrawLineCount { get; set; }

		protected virtual void OnGUI()
		{
			// EditorGUIUtility.wideMode should be set here; otherwise, some EditorGUI will draw poorly (e.g.EditorGUI.MultiFloatField )
			EditorGUIUtility.wideMode = true;
			DrawLineCount = 0;
		}

		protected void DrawEmptyLine(int count)
		{
			DrawLineCount += count;
		}

		protected Rect GetRectAndIterateLine(Rect position)
		{
			return EditorScriptingExtension.GetRectAndIterateLine(this, position);
		}
	}

}