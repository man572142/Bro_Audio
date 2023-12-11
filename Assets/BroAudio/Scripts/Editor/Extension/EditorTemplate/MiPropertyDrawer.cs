using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.Extension
{
	public abstract class MiPropertyDrawer : PropertyDrawer, IEditorDrawLineCounter
	{
		public abstract float SingleLineSpace { get; }
		public int DrawLineCount { get; set; }
        public float Offset { get; set; }
        public bool IsEnable { get; protected set; }

		protected virtual void OnEnable()
		{
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// EditorGUIUtility.wideMode should be set here; otherwise, some EditorGUI will draw poorly (e.g.EditorGUI.MultiFloatField )
			EditorGUIUtility.wideMode = true;
			DrawLineCount = 0;
			Offset = 0f;

			if (!IsEnable)
			{
				OnEnable();
				IsEnable = true;
			}
		}

		protected void DrawEmptyLine(int count)
		{
			DrawLineCount += count;
		}

		protected Rect GetNextLineRect(Rect position)
		{
			return EditorScriptingExtension.GetNextLineRect(this,position);
		}

		protected Rect GetRectAndIterateLine(Rect position)
		{
			return GetRectAndIterateLine(position, 0);
		}

        protected Rect GetRectAndIterateLine(Rect position, int extraLines)
        {
			Rect rect = EditorScriptingExtension.GetRectAndIterateLine(this, position);
            DrawEmptyLine(extraLines);
			return rect;
        }
    }
}