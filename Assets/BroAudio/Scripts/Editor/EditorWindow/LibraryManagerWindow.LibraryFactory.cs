using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEngine;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
	public partial class LibraryManagerWindow : EditorWindow
	{
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
			if (Event.current.type == EventType.DragPerform && librariesRect.Scoping(position).Contains(Event.current.mousePosition))
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

                AudioAssetEditor tempEditor = CreateAsset(BroName.TempAssetName,BroAudioType.None);	

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
								CreateNewEntity(tempEditor, clip);
                            }
                            break;
                        case MultiClipsImportOption.Cancel:
							// Do Nothing
                            break;
                        case MultiClipsImportOption.OneForAll:
                            CreateNewEntity(tempEditor, clips);
                            break;
                    }
                }
                else if(clips.Count == 1)
				{
					CreateNewEntity(tempEditor, clips[0]);
				}
				tempEditor.Verify();
				tempEditor.serializedObject.ApplyModifiedProperties();
			}
        }

        private void CreateNewEntity(AudioAssetEditor editor, AudioClip clip)
        {
            SerializedProperty entity = editor.CreateNewEntity();
            SerializedProperty clipListProp = entity.FindPropertyRelative(nameof(AudioLibrary.Clips));

			SetClipList(clipListProp, 0, clip);
		}

		private void CreateNewEntity(AudioAssetEditor editor, List<AudioClip> clips)
		{
			SerializedProperty entity = editor.CreateNewEntity();
			SerializedProperty clipListProp = entity.FindPropertyRelative(nameof(AudioLibrary.Clips));

			for(int i = 0; i < clips.Count;i++)
			{
				SetClipList(clipListProp, i, clips[i]);
			}
		}

		private void SetClipList(SerializedProperty clipListProp, int index , AudioClip clip)
		{
			clipListProp.InsertArrayElementAtIndex(index);
			SerializedProperty elementProp = clipListProp.GetArrayElementAtIndex(index);
			elementProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip)).objectReferenceValue = clip;
			elementProp.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = AudioConstant.FullVolume;
		}

		private void ToggleTempGuidingFlash(bool hasAssetName)
		{
			if(!hasAssetName && !_flasingHelper.IsUpdating)
			{
				_flasingHelper.Start();
			}
			else if (hasAssetName && _flasingHelper.IsUpdating)
			{
				_flasingHelper.End();
			}
		}

		private void DrawFlashingReminder(Rect headerRect)
		{
			GUI.DrawTexture(headerRect, Texture2D.whiteTexture, ScaleMode.ScaleAndCrop, true, 0f, _flasingHelper.DisplayColor, 0f, 4f);
		}
	}
}