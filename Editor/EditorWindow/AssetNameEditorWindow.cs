using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
	public class AssetNameEditorWindow : EditorWindow
	{
		public Vector2 WindowSize => new Vector2(250f, 100f);

		public Action<string> OnConfirm;
		public List<string> UsedAssetsName = null;

		private string _assetName = string.Empty;
		private BroInstructionHelper _instruction = new BroInstructionHelper();

        public static void ShowWindow(List<string> usedAssetName,Action<string> onConfirm)
		{
			AssetNameEditorWindow window = GetWindow<AssetNameEditorWindow>();
			window.minSize = window.WindowSize;
			window.maxSize = window.WindowSize;
			window.titleContent = new GUIContent("New Asset");
			window.OnConfirm = onConfirm;
			window.UsedAssetsName = usedAssetName;
			window.ShowModalUtility();
		}

		private void OnGUI()
		{
			GUI.enabled = true;
			_assetName = EditorGUILayout.TextField(_assetName, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
			if(!DrawAssetNameValidation(_assetName) || !DrawTempNameValidation() || !DrawDuplicateValidation())
			{
				GUI.enabled = false;
			}

			GUILayout.FlexibleSpace();
			
			if(GUILayout.Button("OK"))
			{
				OnConfirm?.Invoke(_assetName.TrimStartAndEnd());
				Close();
			}
		}

		private bool DrawTempNameValidation()
		{
			if (BroEditorUtility.IsTempReservedName(_assetName))
			{
				string text = String.Format(_instruction.GetText(Instruction.AssetNaming_StartWithTemp),_assetName);
				EditorGUILayout.HelpBox(text, MessageType.Error);
				return false;
			}
			return true;
		}

		private bool DrawDuplicateValidation()
		{
			if (UsedAssetsName != null && UsedAssetsName.Contains(_assetName))
			{
				EditorGUILayout.HelpBox(_instruction.GetText(Instruction.AssetNaming_IsDuplicated), MessageType.Error);
				return false;
			}
			return true;
		}

		public bool DrawAssetNameValidation(string assetName)
		{
			if (BroEditorUtility.IsInvalidName(assetName, out ValidationErrorCode code))
			{
				switch (code)
				{
					case ValidationErrorCode.IsNullOrEmpty:
						EditorGUILayout.HelpBox(_instruction.GetText(Instruction.AssetNaming_IsNullOrEmpty), MessageType.Info);
						return false;
					case ValidationErrorCode.StartWithNumber:
						EditorGUILayout.HelpBox(_instruction.GetText(Instruction.AssetNaming_StartWithNumber), MessageType.Error);
						return false;
					case ValidationErrorCode.ContainsInvalidWord:
						EditorGUILayout.HelpBox(_instruction.GetText(Instruction.AssetNaming_ContainsInvalidWords), MessageType.Error);
						return false;
                    case ValidationErrorCode.ContainsWhiteSpace:
                        EditorGUILayout.HelpBox(_instruction.GetText(Instruction.AssetNaming_ContainsWhiteSpace), MessageType.Error);
						return false;
                }
			}
			return true;
		}
	}
}