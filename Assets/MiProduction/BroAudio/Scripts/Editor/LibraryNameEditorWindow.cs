using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using MiProduction.Extension;

namespace MiProduction.BroAudio
{
	public class LibraryNameEditorWindow : EditorWindow
	{
		public static readonly Vector2 WindowSize = new Vector2(250f, 100f);

		public Action<string> OnConfirm;
		public List<string> CurrentLibrariesName = null;

		private string _libraryName = string.Empty;
		private GUIStyleHelper _guiStyleHelper = GUIStyleHelper.Instance;
		

		public static void ShowWindow(List<string> currentLibrariesName,Action<string> onConfirm)
		{

			EditorWindow window = GetWindow(typeof(LibraryNameEditorWindow));
			window.minSize = WindowSize;
			window.maxSize = WindowSize;
			window.titleContent = new GUIContent("New Library");
			window.Show();

			var instance = window as LibraryNameEditorWindow;
			if (instance != null)
			{
				instance.OnConfirm = onConfirm;
				instance.CurrentLibrariesName = currentLibrariesName;
			}
		}

		private void OnGUI()
		{
			GUI.enabled = true;
			_libraryName = EditorGUILayout.TextField(_libraryName, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
			if(!IsValidName())
			{
				GUI.enabled = false;
			}

			GUILayout.FlexibleSpace();
			
			if(GUILayout.Button("OK"))
			{
				OnConfirm?.Invoke(_libraryName);
				Close();
			}
		}

		private bool IsValidName()
		{
			if (Utility.IsInvalidName(_libraryName, out Utility.ValidationErrorCode code))
			{
				switch (code)
				{
					case Utility.ValidationErrorCode.IsNullOrEmpty:
						EditorGUILayout.HelpBox("Please Enter Library Name", MessageType.Info);
						return false;
					case Utility.ValidationErrorCode.StartWithNumber:
						EditorGUILayout.HelpBox("Name can't start with a number!", MessageType.Error);
						return false;
					case Utility.ValidationErrorCode.ContainsInvalidWord:
						EditorGUILayout.HelpBox("Name contains invalid word!", MessageType.Error);
						return false;
				}
			}
			else
			{
				if (CurrentLibrariesName.Contains(_libraryName))
				{
					EditorGUILayout.HelpBox("Name already exists!", MessageType.Error);
					return false;
				}
				
			}
			return true;
		}
	}

}