using System;
using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public partial class LibraryManagerWindow : EditorWindow
	{
		public const string TempAssetKey = "Temp";
		private enum ComplexDialogDecision	{ OK, Cancel, Alt,}

		private void DrawLibraryFactory(Rect librariesRect)
		{
			HandleDragAndDrop(librariesRect);

			GUILayoutUtility.GetRect(librariesRect.width, librariesRect.height);

			float creationHintHeight = EditorScriptingExtension.FontSizeToPixels(CreationHintFontSize);
			Rect creationRect = new Rect(0f, librariesRect.height * 0.5f - creationHintHeight, librariesRect.width, creationHintHeight);
			string creationHint = _instruction.GetText(Instruction.LibraryManager_CreateEntity).SetSize(CreationHintFontSize).SetColor(GUIStyleHelper.DefaultLabelColor);
			EditorGUI.LabelField(creationRect, creationHint, GUIStyleHelper.MiddleCenterRichText);

			Rect importIcon = new Rect(librariesRect.width * 0.5f, creationRect.y - ImportIconSize, ImportIconSize, ImportIconSize);
			GUI.DrawTexture(importIcon, EditorGUIUtility.IconContent(IconConstant.ImportFile).image);

			float modifyHintHeight = EditorScriptingExtension.FontSizeToPixels(AssetModificationFontSize);
			Rect modifyRect = new Rect(0f, librariesRect.height * 0.5f, librariesRect.width, modifyHintHeight);
			string modifyHint = _instruction.GetText(Instruction.LibraryManager_ModifyAsset).SetSize(AssetModificationFontSize).SetColor(GUIStyleHelper.DefaultLabelColor * 0.8f);
			EditorGUI.LabelField(modifyRect, modifyHint, GUIStyleHelper.MiddleCenterRichText);
		}

		private void HandleDragAndDrop(Rect librariesRect)
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			if (Event.current.type == EventType.DragExited && librariesRect.Scoping(position).Contains(Event.current.mousePosition))
			{
				var objs = DragAndDrop.objectReferences;

				if (objs.Length > 1)
				{
					var decision = (ComplexDialogDecision)EditorUtility.DisplayDialogComplex("Multi-clips import decision", "You have drop more than one audio clips.\nDo you want to create multiple AudioEntities for each clip, or create one AudioEntity to contain them in its clip list?", "Multiple for each", "Cancel", "One for all");


				}
				else if(objs.Length == 1)
				{
					CreateTempEntity();
				}
			}
		}

		private void CreateTempEntity()
		{
			var newAsset = ScriptableObject.CreateInstance(typeof(TempAudioAsset));
			AudioAssetEditor baseEditor = UnityEditor.Editor.CreateEditor(newAsset) as AudioAssetEditor;
			TempAudioAssetEditor tempEditor = baseEditor as TempAudioAssetEditor;
			tempEditor.AddTempEntity();
			// id 及 audiotype都不指定? 
			_assetEditorDict.Add(TempAssetKey, tempEditor);
		}
	} 
}
