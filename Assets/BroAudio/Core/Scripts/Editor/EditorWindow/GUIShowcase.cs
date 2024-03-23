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
                _allCursorTypes = _allCursorTypes ?? (IEnumerable<MouseCursor>)Enum.GetValues(typeof(MouseCursor));
                return _allCursorTypes;
			}
		}

#if BroAudio_DevOnly
        [MenuItem(BroName.MenuItem_BroAudio + "GUI Showcase")]
#endif
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
			using (new EditorGUI.IndentLevelScope())
			{
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Cursor Type".SetSize(25), GUIStyleHelper.RichText);
                DrawEmptyLine(1);

                using (new EditorGUI.IndentLevelScope())
				{
                    foreach (MouseCursor cursorType in AllCursorTypes)
                    {
                        Rect rect = GetRectAndIterateLine(drawPosition);
                        rect.width = CursorTypeWidth;
                        EditorGUI.LabelField(rect, cursorType.ToString());
                        EditorGUIUtility.AddCursorRect(rect, cursorType);
                    }
                }
            }
		}
	}
}