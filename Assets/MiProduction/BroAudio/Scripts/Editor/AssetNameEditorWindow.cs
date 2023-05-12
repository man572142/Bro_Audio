using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using MiProduction.Extension;

namespace MiProduction.BroAudio.AssetEditor
{
	public class AssetNameEditorWindow : EditorWindow
	{
		public static readonly Vector2 WindowSize = new Vector2(250f, 100f);

		public Action<string> OnConfirm;
		public List<string> UsedAssetsName = null;

		private string _libraryName = string.Empty;
		private GUIStyleHelper _guiStyleHelper = GUIStyleHelper.Instance;
		

		public static void ShowWindow(List<string> usedAssetName,Action<string> onConfirm)
		{

			EditorWindow window = GetWindow(typeof(AssetNameEditorWindow));
			window.minSize = WindowSize;
			window.maxSize = WindowSize;
			window.titleContent = new GUIContent("New Asset");
			window.Show();

			var instance = window as AssetNameEditorWindow;
			if (instance != null)
			{
				instance.OnConfirm = onConfirm;
				instance.UsedAssetsName = usedAssetName;
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
						EditorGUILayout.HelpBox("Please Enter Asset Name", MessageType.Info);
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
				if (UsedAssetsName.Contains(_libraryName))
				{
					EditorGUILayout.HelpBox("Name already exists!", MessageType.Error);
					return false;
				}
				
			}
			return true;
		}
	}

}