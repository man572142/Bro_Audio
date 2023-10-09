using System;
using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public partial class LibraryManagerWindow : EditorWindow
	{
		public const string TempAssetKey = "Temp";
		private enum MultiClipsImportOption	
		{ 
			MultipleForEach,
			Cancel,
			OneForAll,
		}

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

                List<AudioClip> clips = new List<AudioClip>();
                foreach (UnityEngine.Object obj in objs)
				{
					if(obj is AudioClip clip)
					{
						clips.Add(clip);
                    }
				}

				if(clips.Count == 0 && objs.Length > 0)
				{
                    BroLog.LogWarning("The file isn't an Audio Clip");
					return;
                }

                TempAudioAssetEditor tempEditor = CreateTempAssetEditor();

                if (clips.Count > 1)
				{
					var option = (MultiClipsImportOption)EditorUtility.DisplayDialogComplex(
						_instruction.GetText(Instruction.LibraryManager_MultiClipsImportTitle), // Title
						_instruction.GetText(Instruction.LibraryManager_MultiClipsImportDialog), //Message
						MultiClipsImportOption.MultipleForEach.ToString(), // OK
						MultiClipsImportOption.Cancel.ToString(), // Cancel
						MultiClipsImportOption.OneForAll.ToString()); // Alt

                    switch (option)
                    {
                        case MultiClipsImportOption.MultipleForEach:
                            foreach (AudioClip clip in clips)
							{
								CreateTempEntity(tempEditor, clip);
                            }
                            break;
                        case MultiClipsImportOption.Cancel:
							// Do Nothing
                            break;
                        case MultiClipsImportOption.OneForAll:
                            CreateTempEntity(tempEditor, clips);
                            break;
                    }
                }
                else if(clips.Count == 1)
				{
					CreateTempEntity(tempEditor, clips[0]);
				}
            }
        }

        private void CreateTempEntity(TempAudioAssetEditor tempEditor, AudioClip clip)
        {
            SerializedProperty tempEntity = tempEditor.CreateTempEntity();
            SerializedProperty clipListProp = tempEntity.FindPropertyRelative(nameof(AudioLibrary.Clips));

            clipListProp.InsertArrayElementAtIndex(0);
            clipListProp.GetArrayElementAtIndex(0).FindPropertyRelative(nameof(BroAudioClip.AudioClip)).objectReferenceValue = clip;

        }

		private void CreateTempEntity(TempAudioAssetEditor tempEditor,List<AudioClip> clips)
		{
			SerializedProperty tempEntity = tempEditor.CreateTempEntity();
			SerializedProperty clipListProp = tempEntity.FindPropertyRelative(nameof(AudioLibrary.Clips));

			for(int i = 0; i < clips.Count;i++)
			{
                clipListProp.InsertArrayElementAtIndex(i);
                clipListProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BroAudioClip.AudioClip)).objectReferenceValue = clips[i];
            }
        }

        private TempAudioAssetEditor CreateTempAssetEditor()
        {
            var newAsset = ScriptableObject.CreateInstance(typeof(TempAudioAsset));
            AudioAssetEditor baseEditor = UnityEditor.Editor.CreateEditor(newAsset) as AudioAssetEditor;
            TempAudioAssetEditor tempEditor = baseEditor as TempAudioAssetEditor;
            _assetEditorDict.Add(TempAssetKey, tempEditor);
            return tempEditor;
        }
    }
}
