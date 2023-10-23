using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using static Ami.Extension.EditorScriptingExtension;
using Ami.BroAudio.Editor.Setting;
using System;

namespace Ami.BroAudio.Editor
{
	public class LibraryTreeViewWindow : EditorWindow
	{
		public const float Padding = 6f;

		[SerializeField] private TreeViewState _treeViewState;

		private LibraryTreeView _treeView;

#if BroAudio_DevOnly
		[MenuItem("BroAudio/TreeView", priority = 13)]
#endif
		public static void ShowWindow()
		{
			var window = GetWindow<LibraryTreeViewWindow>();
			window.minSize = new Vector2(960f, 540f);
			Texture icon = Resources.Load<Texture>(BroAudioGUISetting.LogoPath);
			window.titleContent = new GUIContent("Library Manager",icon);
			window.Show();
		}

		private void OnEnable()
		{
			if(_treeViewState == null)
			{
				_treeViewState = new TreeViewState();
			}

			_treeView = new LibraryTreeView(_treeViewState,null);
		}

		private void OnGUI()
		{
			Rect drawPos = new Rect(Vector2.one * Padding, position.size - (Vector2.one * Padding * 2));

			SplitRectHorizontal(drawPos, 0.4f, 10f, out Rect hierarchyRect, out Rect inspectorRect);
			
			if (Event.current.type == EventType.Repaint)
			{
				GUIStyle frameBox = "FrameBox";
				frameBox.Draw(hierarchyRect, false, false, false, false);
				frameBox.Draw(inspectorRect, false, false, false, false);
			}
			DrawHierarchy(hierarchyRect);
			DrawInspector(inspectorRect);
		}

		private void DrawHierarchy(Rect hierarchyRect)
		{
			Rect treeViewRect = new Rect(hierarchyRect);
			treeViewRect.position += Vector2.one * Padding;
			treeViewRect.size -= Vector2.one * Padding * 2;

			_treeView.OnGUI(treeViewRect);
		}

		private void DrawInspector(Rect inspectorRect)
		{
			
		}
	}
}