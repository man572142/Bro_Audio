using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using Ami.Extension;
using Ami.BroAudio.Editor.Setting;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Utility;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
	public partial class LibraryManagerWindow : EditorWindow
	{
		public const int CreationHintFontSize = 25;
		public const int AssetModificationFontSize = 15;
		public const float ImportIconSize = 30f;
		public const float BackButtonSize = 28f;

		public event Action OnCloseLibraryManagerWindow;
		public event Action OnSelectAsset;

		private readonly Vector2 _librariesHeaderSize = new Vector2(200f,EditorGUIUtility.singleLineHeight * 2);
		private List<string> _allAssetGUIDs = null;
		private ReorderableList _assetReorderableList = null;
		private int _currSelectedAssetIndex = -1;
		private GenericMenu _createAssetOption = null;
		private GenericMenu _changeAudioTypeOption = null;

		private Dictionary<string, AudioAssetEditor> _assetEditorDict = new Dictionary<string, AudioAssetEditor>();

		private Vector2 _assetListScrollPos = Vector2.zero;
		private Vector2 _librariesScrollPos = Vector2.zero;

		private GapDrawingHelper _verticalGapDrawer = new GapDrawingHelper();
		private LibraryIDController _libraryIdGenerator = new LibraryIDController();
		private BroInstructionHelper _instruction = new BroInstructionHelper();

		[MenuItem(LibraryManagerMenuPath, false,LibraryManagerMenuIndex)]
		public static LibraryManagerWindow ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(LibraryManagerWindow));
			window.minSize = BroAudioGUISetting.MinWindowSize;
			window.titleContent = new GUIContent(BroName.MenuItem_LibraryManager);
			window.Show();
			return window as LibraryManagerWindow;
		}

		public static void OpenFromAssetFile(string guid)
		{
			LibraryManagerWindow window = ShowWindow();
			window.SelectAsset(guid);
		}

		public void SelectAsset(string guid)
		{
			int index = _allAssetGUIDs.IndexOf(guid);
			if(index >= 0)
			{
				_assetReorderableList.index = index;
				OnSelect(_assetReorderableList);
			}
		}

		public void RemoveAssetEditor(string guid)
		{
			if (_assetEditorDict.TryGetValue(guid, out var editor))
			{
				DestroyImmediate(editor);
			}
			_assetEditorDict.Remove(guid);
			_allAssetGUIDs.Remove(guid);
		}

		private void OnFocus()
		{
			EditorPlayAudioClip.PlaybackIndicator.OnUpdate -= Repaint;
			EditorPlayAudioClip.PlaybackIndicator.OnUpdate += Repaint;
			EditorPlayAudioClip.PlaybackIndicator.OnEnd -= Repaint;
			EditorPlayAudioClip.PlaybackIndicator.OnEnd += Repaint;
		}

		private void OnLostFocus()
		{
			EditorPlayAudioClip.StopAllClips();
			EditorPlayAudioClip.PlaybackIndicator.OnUpdate -= Repaint;
			EditorPlayAudioClip.PlaybackIndicator.OnEnd -= Repaint;
		}

		private void OnEnable()
		{
			_allAssetGUIDs = GetGUIDListFromJson();
			_instruction.Init();

			InitEditorDictionary();
			InitReorderableList();

			_createAssetOption = CreateAudioTypeGenericMenu(Instruction.LibraryManager_CreateAssetWithAudioType, ShowCreateAssetAskName);
			_changeAudioTypeOption = CreateAudioTypeGenericMenu(Instruction.LibraryManager_ChangeAssetAudioType, OnChangeAssetAudioType);
		}

		private void OnDisable()
		{
			OnCloseLibraryManagerWindow?.Invoke();
			foreach (AudioAssetEditor editor in _assetEditorDict.Values)
			{
				DestroyImmediate(editor);
			}	
		}

		#region Initialization
		private void InitEditorDictionary()
		{
			_assetEditorDict.Clear();
			foreach (string guid in _allAssetGUIDs)
			{
				if (!string.IsNullOrEmpty(guid) && !_assetEditorDict.ContainsKey(guid))
				{
					AudioAssetEditor editor = CreateAssetEditor(guid);
					_assetEditorDict.Add(guid, editor);
				}
			}
		}

		private AudioAssetEditor CreateAssetEditor(string guid , string assetName = null, BroAudioType audioType = default)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject))) as AudioAssetEditor;
			if(string.IsNullOrEmpty(editor.Asset.AssetName))
			{
				string assetNamePropertyPath = EditorScriptingExtension.GetBackingFieldName(nameof(IAudioAsset.AssetName));
				editor.serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

				string assetGUIDPropertyPath = EditorScriptingExtension.GetFieldName(nameof(IAudioAsset.AssetGUID));
				editor.serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

				string audioTypePropertyPath = EditorScriptingExtension.GetBackingFieldName(nameof(IAudioAsset.AudioType));
				editor.serializedObject.FindProperty(audioTypePropertyPath).enumValueIndex = audioType.GetSerializedEnumIndex();

				editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			editor.SetIDGenerator(_libraryIdGenerator);
			return editor;
		}

		private void InitReorderableList()
		{
			_assetReorderableList = new ReorderableList(_allAssetGUIDs, typeof(string));
			_assetReorderableList.drawHeaderCallback = OnDrawHeader;
			_assetReorderableList.onAddDropdownCallback = OnAddDropdown;
			_assetReorderableList.onRemoveCallback = OnRemove;
			_assetReorderableList.drawElementCallback = OnDrawElement;
			_assetReorderableList.onSelectCallback = OnSelect;

			void OnDrawHeader(Rect rect)
			{
				EditorGUI.LabelField(rect, "Asset List");
			}

			void OnAddDropdown(Rect buttonRect, ReorderableList list)
			{
				_createAssetOption.DropDown(buttonRect);
			}

			void OnRemove(ReorderableList list)
			{
				string path = AssetDatabase.GUIDToAssetPath(_allAssetGUIDs[list.index]);
				AssetDatabase.DeleteAsset(path);
				AssetDatabase.Refresh();
				// AssetModificationEditor will do the rest
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				if(_assetEditorDict.TryGetValue(_allAssetGUIDs[index],out var editor))
				{
					if(editor.Asset == null)
						return;

					EditorScriptingExtension.SplitRectHorizontal(rect, 0.7f, 0f, out Rect labelRect, out Rect audioTypeRect);

					EditorGUI.LabelField(labelRect, editor.Asset.AssetName);

					EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(editor.Asset.AudioType));
					EditorGUI.LabelField(audioTypeRect, editor.Asset.AudioType.ToString(), GUIStyleHelper.MiddleCenterText);
				}
			}
		}

		private void OnSelect(ReorderableList list)
		{
			if (list.index != _currSelectedAssetIndex)
			{
				OnSelectAsset?.Invoke();
				_currSelectedAssetIndex = list.index;
				EditorPlayAudioClip.StopAllClips();
			}
		}

		private GenericMenu CreateAudioTypeGenericMenu(Instruction instruction, GenericMenu.MenuFunction2 onClickOption)
		{
			GenericMenu menu = new GenericMenu();
			GUIContent text = new GUIContent(_instruction.GetText(instruction));
			menu.AddItem(text, false, null);
			menu.AddSeparator("");

			ForeachConcreteAudioType((audioType) =>
			{
				GUIContent optionName = new GUIContent(audioType.ToString());
				menu.AddItem(optionName, false, onClickOption, audioType);
			});

			return menu;
		}

		private void OnChangeAssetAudioType(object type)
		{
			if (TryGetCurrentAssetEditor(out var editor))
			{
				editor.Asset.AudioType = (BroAudioType)type;
				editor.serializedObject.ApplyModifiedProperties();
				Repaint();
			}
		}
		#endregion

		private void ShowCreateAssetAskName(object type)
		{
			// In the following case. List has better performance than IEnumerable , even with a ToList() method.
			List<string> assetNames = _assetEditorDict.Values.Select(x => x.Asset.AssetName).ToList();
			AssetNameEditorWindow.ShowWindow(assetNames, (assetName)=> OnCreateAsset(assetName, (BroAudioType)type));
		}

		private void OnCreateAsset(string libraryName, BroAudioType audioType)
		{
			if(string.IsNullOrEmpty(AssetOutputPath))
			{
				return;
			}

			string fileName = libraryName + ".asset";
			string path = GetFilePath(AssetOutputPath, fileName);

			var newAsset = ScriptableObject.CreateInstance(typeof(AudioAsset));
			AssetDatabase.CreateAsset(newAsset, path);
			AddToSoundManager(newAsset);
			AssetDatabase.SaveAssets();

			string guid = AssetDatabase.AssetPathToGUID(path);

			_allAssetGUIDs.Add(guid);
			_assetEditorDict.Add(guid, CreateAssetEditor(guid, libraryName,audioType));

			WriteGuidToCoreData(_allAssetGUIDs);	
		}

		private bool TryGetCurrentAssetEditor(out AudioAssetEditor editor)
		{
			if (_allAssetGUIDs.Count > 0 && _assetReorderableList.index >= 0)
			{
				int index = Mathf.Clamp(_assetReorderableList.index, 0, _allAssetGUIDs.Count - 1);
				if (_assetEditorDict.TryGetValue(_allAssetGUIDs[index], out editor))
				{
					return true;
				}
			}
			else if(_assetEditorDict.TryGetValue(TempAssetKey, out editor))
			{
				return true;
			}
			return false;
		}

		private void OnGUI()
		{
			_verticalGapDrawer.DrawLineCount = 0;

			EditorGUILayout.BeginHorizontal();
			{                
                GUILayout.Space(_verticalGapDrawer.GetSpace());
				EditorScriptingExtension.SplitRectHorizontal(position, 0.7f, _verticalGapDrawer.SingleLineSpace, out Rect librariesRect, out Rect assetListRect);

				librariesRect.width -= _verticalGapDrawer.GetTotalSpace();
				DrawLibrariesList(librariesRect);

				float offsetX = _verticalGapDrawer.GetTotalSpace() + GUI.skin.box.padding.left;
				float offsetY = ReorderableList.Defaults.padding + GUI.skin.box.padding.top -1f ;
				DrawClipPropertiesHelper.DrawPlaybackIndicator(librariesRect.Scoping(position, new Vector2(offsetX,offsetY)), -_librariesScrollPos);

				GUILayout.Space(_verticalGapDrawer.GetSpace());

				EditorGUILayout.BeginVertical();
				{
                    DrawAssetList(assetListRect.width - (_verticalGapDrawer.GetTotalSpace() - _verticalGapDrawer.SingleLineSpace));
                }
				EditorGUILayout.EndVertical();
			}
			
			EditorGUILayout.EndHorizontal();
		}

		private void DrawAssetList(float width)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box,GUILayout.Width(width));
			{
				_assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
				{
					_assetReorderableList.DoLayoutList();
				}
				EditorGUILayout.EndScrollView();

				EditorGUILayout.BeginHorizontal();
				{
					if (TryGetCurrentAssetEditor(out var editor))
					{
						DrawLibraryStateMessage(editor);
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			void DrawLibraryStateMessage(AudioAssetEditor editor)
			{
				LibraryState state = editor.GetLibraryState(out string dataName);
				string assetName = editor.Asset.AssetName.ToBold().SetColor(Color.white);
				dataName = dataName.ToBold().SetColor(Color.white);
				string text = string.Empty;
                switch (state)
				{
					case LibraryState.HasEmptyName:
                        text = _instruction.GetText(Instruction.LibraryState_IsNullOrEmpty);
						EditorScriptingExtension.RichTextHelpBox(String.Format(text,assetName), MessageType.Error);
						break;
					case LibraryState.HasDuplicateName:
                        text = _instruction.GetText(Instruction.LibraryState_IsDuplicated);
                        EditorScriptingExtension.RichTextHelpBox(String.Format(text,dataName,assetName), MessageType.Error);
						break;
					case LibraryState.HasInvalidName:
                        text = _instruction.GetText(Instruction.LibraryState_ContainsInvalidWords);
                        EditorScriptingExtension.RichTextHelpBox(String.Format(text, dataName, assetName), MessageType.Error);
						break;
					case LibraryState.Fine:
                        text = _instruction.GetText(Instruction.LibraryState_Fine);
                        EditorScriptingExtension.RichTextHelpBox(text, IconConstant.LibraryWorkdFine);
						break;
				}
			}
		}

		private void DrawLibrariesList(Rect librariesRect)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(librariesRect.width));
			{
				if (TryGetCurrentAssetEditor(out var editor))
				{
					_librariesScrollPos = EditorGUILayout.BeginScrollView(_librariesScrollPos);
					{
						DrawLibraryHeader(editor.Asset);
						editor.DrawLibraries();
					}
					EditorGUILayout.EndScrollView();
				}
				else
				{
					DrawLibraryFactory(librariesRect);
				}
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawLibraryHeader(IAudioAsset asset)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if(GUILayout.Button(EditorGUIUtility.IconContent(IconConstant.BackButton),GUILayout.Width(BackButtonSize),GUILayout.Height(BackButtonSize)))
				{
					_assetReorderableList.index = -1;
					// TODO: quit temp asset
				}

				GUILayout.Space(10f);

				// The ReorderableList default header background GUIStyle has set fixedHeight to non-0 and stretchHeight to false, which is unreasonable...
				// Use another style or Draw it manually could solve the problem and accept more customization.
				Rect headerRect = GUILayoutUtility.GetRect(_librariesHeaderSize.x, _librariesHeaderSize.y);
				headerRect.width = _librariesHeaderSize.x;
				GUIStyle header = new GUIStyle(GUI.skin.window);
				if (Event.current.type == EventType.Repaint)
				{
					header.Draw(headerRect, false, false, false, false);
				}
				GUIStyle wrappableStyle = new GUIStyle(GUIStyleHelper.MiddleCenterRichText);
				wrappableStyle.wordWrap = true;
				EditorGUI.LabelField(headerRect, asset.AssetName.SetColor(GUIStyleHelper.DefaultLabelColor).SetSize(16), wrappableStyle);

				GUILayout.FlexibleSpace();

				Rect audioTypeRect = GUILayoutUtility.GetRect(100f, 25f);
				audioTypeRect.y += _librariesHeaderSize.y - audioTypeRect.height;
				GUIContent audioTypeGUI = new GUIContent(asset.AudioType.ToString(),"Click to change audio type");
				if (GUI.Button(audioTypeRect, audioTypeGUI))
				{
					_changeAudioTypeOption.DropDown(audioTypeRect);
				}
				EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(asset.AudioType));

			}
			EditorGUILayout.EndHorizontal();
		}
	}
}