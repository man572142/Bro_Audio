using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Ami.Extension;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
	public class AssetNameEditorWindow : EditorWindow
	{
		public static readonly Vector2 WindowSize = new Vector2(250f, 100f);

		public Action<string> OnConfirm;
		public List<string> UsedAssetsName = null;

		private string _assetName = string.Empty;
		private BroInstructionHelper _instruction = null;

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
				instance.Init();
			}
		}

		public void Init()
		{
			if(_instruction == null) 
				_instruction = new BroInstructionHelper();
			_instruction.Init();
		}

		private void OnGUI()
		{
			GUI.enabled = true;
			_assetName = EditorGUILayout.TextField(_assetName, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));
			if(!DrawAssetNameValidation(_assetName,_instruction) && DrawTempNameValidation() && DrawDuplicateValidation())
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
			if(_assetName == BroName.TempAssetName || 
			(_assetName.StartsWith(BroName.TempAssetName) && Char.IsNumber(_assetName[BroName.TempAssetName.Length])))
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

		public static bool DrawAssetNameValidation(string assetName,BroInstructionHelper instruction)
		{
			// todo: might need another helpbox validation while editing asset name
			if (Utility.IsInvalidName(assetName, out Utility.ValidationErrorCode code))
			{
				switch (code)
				{
					case Utility.ValidationErrorCode.IsNullOrEmpty:
						EditorGUILayout.HelpBox(instruction.GetText(Instruction.AssetNaming_IsNullOrEmpty), MessageType.Info);
						return false;
					case Utility.ValidationErrorCode.StartWithNumber:
						EditorGUILayout.HelpBox(instruction.GetText(Instruction.AssetNaming_StartWithNumber), MessageType.Error);
						return false;
					case Utility.ValidationErrorCode.ContainsInvalidWord:
						EditorGUILayout.HelpBox(instruction.GetText(Instruction.AssetNaming_ContainsInvalidWords), MessageType.Error);
						return false;
                    case Utility.ValidationErrorCode.ContainsWhiteSpace:
                        EditorGUILayout.HelpBox(instruction.GetText(Instruction.AssetNaming_ContainsWhiteSpace), MessageType.Error);
                        return false;
                }
			}
			return true;
		}
	}
}