using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEngine;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

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

		public const float CenterLineGap = 30f;
		public const float CenterLength = 130f;
		public const float BackgroundLogoSize = 500f;
		public const int DragAndDropFontSize = 35;

        private int _pickerID = -1;
		private Texture _backgroundLogo = null;
		private Material _backgroundLogoMat = null;

        public Vector2 BrowseButtonSize => new Vector2(80f,30f);
		
		private void DrawEntityFactory(Rect factoryRect)
		{
			EditorGUILayout.BeginVertical();
			{
				HandleDragAndDrop(factoryRect);
				DrawBackgroundLogo(factoryRect);

				GUILayout.Space(factoryRect.height * 0.4f);
				EditorGUILayout.LabelField("Drag & Drop".SetSize(DragAndDropFontSize).SetColor(DefaultLabelColor), GUIStyleHelper.MiddleCenterRichText);
				GUILayout.Space(15f);
				EditorGUILayout.LabelField("or".SetColor(DefaultLabelColor), GUIStyleHelper.MiddleCenterRichText);

				Rect centerLineRect = GUILayoutUtility.GetLastRect();
				using (new Handles.DrawingScope(Color.grey))
				{
					float middleX = centerLineRect.xMin + centerLineRect.width * 0.5f;
					float middleY = centerLineRect.yMin + centerLineRect.height * 0.5f;
					Handles.DrawAAPolyLine(2f, new Vector3(middleX - CenterLineGap - CenterLength, middleY), new Vector3(middleX - CenterLineGap, middleY));
					Handles.DrawAAPolyLine(2f, new Vector3(middleX + CenterLineGap + CenterLength, middleY), new Vector3(middleX + CenterLineGap, middleY));
				}

				GUILayout.Space(15f);
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Browse", GUILayout.Width(BrowseButtonSize.x), GUILayout.Height(BrowseButtonSize.y)))
					{
						_pickerID = EditorGUIUtility.GetControlID(FocusType.Passive);
						EditorGUIUtility.ShowObjectPicker<AudioClip>(null, false, string.Empty, _pickerID);
					}
					GUILayout.FlexibleSpace();
				}
				EditorGUILayout.EndHorizontal();
				HandleObjectPicker();
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawBackgroundLogo(Rect entitiesRect)
		{
			if (Event.current.type == EventType.Repaint)
			{
				Vector2 audioIconSize = new Vector2(BackgroundLogoSize, BackgroundLogoSize);
				float offsetX = DefaultLayoutPadding * 2f;
				Vector2 logoPos = entitiesRect.size * 0.5f - audioIconSize * 0.5f + new Vector2(offsetX, 0f);
				Rect audioIconRect = new Rect(logoPos, audioIconSize);
                _backgroundLogo = _backgroundLogo ? _backgroundLogo : Resources.Load<Texture>(BroName.TransparentLogoPath);
                _backgroundLogoMat = _backgroundLogoMat ? _backgroundLogoMat : (Material)EditorGUIUtility.LoadRequired("Inspectors/InactiveGUI.mat");
				EditorGUI.DrawPreviewTexture(audioIconRect, _backgroundLogo, _backgroundLogoMat, ScaleMode.ScaleToFit);
			}
		}

		private void HandleObjectPicker()
		{
			if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorClosed" 
				&& EditorGUIUtility.GetObjectPickerControlID() == _pickerID)
			{
				AudioClip audioClip = EditorGUIUtility.GetObjectPickerObject() as AudioClip;
				if (audioClip)
				{
					AudioAssetEditor tempEditor = CreateAsset(BroName.TempAssetName);
					CreateNewEntity(tempEditor, audioClip);
				}
				_pickerID = -1;
			}
		}

		private void HandleDragAndDrop(Rect entitiesRect)
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			if (Event.current.type == EventType.DragPerform && entitiesRect.Scoping(position).Contains(Event.current.mousePosition))
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
                    Debug.LogWarning(Utility.LogTitle + "The file isn't an Audio Clip");
					return;
                }

                AudioAssetEditor tempEditor = null;	
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
								tempEditor = CreateAsset(BroName.TempAssetName);
                                CreateNewEntity(tempEditor, clip);
                            }
                            break;
                        case MultiClipsImportOption.Cancel:
							return;
                        case MultiClipsImportOption.OneForAll:
                            tempEditor = CreateAsset(BroName.TempAssetName);
                            CreateNewEntity(tempEditor, clips);
                            break;
                    }
                }
                else if(clips.Count == 1)
				{
                    tempEditor = CreateAsset(BroName.TempAssetName);
                    CreateNewEntity(tempEditor, clips[0]);
				}

				if(tempEditor != null)
				{
                    tempEditor.Verify();
                    tempEditor.serializedObject.ApplyModifiedProperties();
                }
			}
        }

        private void CreateNewEntity(AudioAssetEditor editor, AudioClip clip)
        {
            SerializedProperty entity = editor.CreateNewEntity();
			entity.FindPropertyRelative(EditorScriptingExtension.GetBackingFieldName(nameof(AudioEntity.Name))).stringValue = clip.name;
            SerializedProperty clipListProp = entity.FindPropertyRelative(nameof(AudioEntity.Clips));

			editor.SetClipList(clipListProp, 0, clip);
            _isInEntitiesEditMode = true;
        }

		private void CreateNewEntity(AudioAssetEditor editor, List<AudioClip> clips)
		{
			SerializedProperty entity = editor.CreateNewEntity();
			SerializedProperty clipListProp = entity.FindPropertyRelative(nameof(AudioEntity.Clips));

			for(int i = 0; i < clips.Count;i++)
			{
				editor.SetClipList(clipListProp, i, clips[i]);
			}
			_isInEntitiesEditMode = true;

        }

		//private void ToggleTempGuidingFlash(bool hasAssetName)
		//{
		//	if (!hasAssetName && !_flasingHelper.IsUpdating)
		//	{
		//		_flasingHelper.Start();
		//	}
		//	else if (hasAssetName && _flasingHelper.IsUpdating)
		//	{
		//		_flasingHelper.End();
		//	}
		//}

		//private void DrawFlashingReminder(Rect headerRect)
		//{
		//	GUI.DrawTexture(headerRect, Texture2D.whiteTexture, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 4f);
		//}
	}
}