using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using Ami.BroAudio.Tools;

namespace Ami.Extension
{
	public class GUIShowcase : MiEditorWindow
	{
		public const float CursorTypeWidth = 200f;
		public const float Gap = 10f;
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 5f;

		private IEnumerable<MouseCursor> _allCursorTypes = null;

		public IEnumerable<MouseCursor> AllCursorTypes
		{
			get
			{
				if (_allCursorTypes == null)
				{
					_allCursorTypes = (IEnumerable<MouseCursor>)Enum.GetValues(typeof(MouseCursor));
				}
				return _allCursorTypes;
			}
		}

		[MenuItem(BroName.MenuItem_BroAudio + "/GUI Showcase")]
		public static void ShowWindow()
		{
			EditorWindow window = EditorWindow.GetWindow<GUIShowcase>();
			window.minSize = new Vector2(960f, 540f);
			window.Show();
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			Rect drawPosition = new Rect(Gap,0f, position.width,position.height);
			DrawEmptyLine(1);

			if (Event.current.type == EventType.Repaint)
			{
				Rect cursorWindow = new Rect(Gap,SingleLineSpace * DrawLineCount,CursorTypeWidth,position.height - SingleLineSpace - Gap);
				GUI.skin.window.Draw(cursorWindow, false, false, false, false);
			}
			EditorGUI.indentLevel++;
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Cursor Type".SetSize(25), GUIStyleHelper.RichText);
			DrawEmptyLine(1);

			EditorGUI.indentLevel++;
			foreach (MouseCursor cursorType in AllCursorTypes)
			{
				Rect rect = GetRectAndIterateLine(drawPosition);
				rect.width = CursorTypeWidth;
				EditorGUI.LabelField(rect, cursorType.ToString());
				EditorGUIUtility.AddCursorRect(rect, cursorType);
			}
			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;

		}
	}

}