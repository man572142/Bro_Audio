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
using System.IO;

namespace Ami.BroAudio.Editor
{
	public partial class LibraryManagerWindow : EditorWindow
	{
		public const int CreationHintFontSize = 25;
		public const int AssetModificationFontSize = 15;
		public const int AssetNameFontSize = 16;
		public const float ImportIconSize = 30f;
		public const float BackButtonSize = 28f;

		public event Action OnCloseLibraryManagerWindow;
		public event Action OnSelectAsset;

		private readonly Vector2 _librariesHeaderSize = new Vector2(200f,EditorGUIUtility.singleLineHeight * 2);
		private readonly Vector2 _headerAudioTypeSize = new Vector2(100f, 25f);
		private List<string> _allAssetGUIDs = null;
		private ReorderableList _assetReorderableList = null;
		private int _currSelectedAssetIndex = -1;
		private GenericMenu _createAssetOption = null;
		private GenericMenu _changeAudioTypeOption = null;

		private Dictionary<string, AudioAssetEditor> _assetEditorDict = new Dictionary<string, AudioAssetEditor>();

		private Vector2 _assetListScrollPos = Vector2.zero;
		private Vector2 _librariesScrollPos = Vector2.zero;

		private GapDrawingHelper _verticalGapDrawer = new GapDrawingHelper();
		private BroInstructionHelper _instruction = new BroInstructionHelper();
		private EditorFlashingHelper _flasingHelper = new EditorFlashingHelper(Color.white,1f,Ease.InCubic);

		public float DefaultLayoutPadding => GUI.skin.box.padding.top;

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

			_flasingHelper.OnUpdate += Repaint;
		}

		private void OnLostFocus()
		{
			EditorPlayAudioClip.StopAllClips();
			EditorPlayAudioClip.PlaybackIndicator.OnUpdate -= Repaint;
			EditorPlayAudioClip.PlaybackIndicator.OnEnd -= Repaint;

			_flasingHelper.OnUpdate -= Repaint;
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
					string assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
					AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(asset, typeof(AudioAssetEditor)) as AudioAssetEditor;
					editor.Init(guid);
					_assetEditorDict.Add(guid, editor);
				}
			}
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
				// AssetPostprocessorEditor will do the rest
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
				editor.SetAudioType((BroAudioType)type);
				editor.serializedObject.ApplyModifiedProperties();
				// TODO: 換了之後其實裡面的ID還是舊的AudioType
			}
		}
		#endregion

		private void ShowCreateAssetAskName(object type)
		{
			// In the following case. List has better performance than IEnumerable , even with a ToList() method.
			List<string> assetNames = _assetEditorDict.Values.Select(x => x.Asset.AssetName).ToList();
			AssetNameEditorWindow.ShowWindow(assetNames, (assetName)=> CreateAsset(assetName, (BroAudioType)type));
		}

		private AudioAssetEditor CreateAsset(string libraryName, BroAudioType audioType)
		{
			if(!TryGetNewPath(libraryName,out string path,out string newName))
			{
				return null;
			}

			var newAsset = ScriptableObject.CreateInstance(typeof(AudioAsset));
			AssetDatabase.CreateAsset(newAsset, path);
			if(audioType != BroAudioType.None)
			{
				AddToSoundManager(newAsset);
			}
			AssetDatabase.SaveAssets();

			AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(newAsset, typeof(AudioAssetEditor)) as AudioAssetEditor;
			string guid = AssetDatabase.AssetPathToGUID(path);
			editor.Init(guid, newName, audioType);
			_assetEditorDict.Add(guid, editor);
			_allAssetGUIDs.Add(guid);

			WriteGuidToCoreData(_allAssetGUIDs);
			_assetReorderableList.index = _assetReorderableList.count - 1;
			return editor;
		}

		private bool TryGetNewPath(string libraryName,out string path,out string newName)
		{
			path = string.Empty;
			newName = libraryName;
			if (!string.IsNullOrEmpty(AssetOutputPath))
			{
				int index = 0;
				path = GetNewAssetPath(libraryName);
				while(File.Exists(path))
				{
					index++;
					newName = libraryName + index.ToString();
					path = GetNewAssetPath(newName);
				}
				return true;
			}
			return false;

			string GetNewAssetPath(string fileName)
			{
				return GetFilePath(AssetOutputPath, fileName + ".asset");
			}
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
			else if(_assetEditorDict.TryGetValue(TempAssetName, out editor))
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
				librariesRect.height -= DefaultLayoutPadding * 2;
				DrawLibrariesList(librariesRect);

				float offsetX = _verticalGapDrawer.GetTotalSpace() + GUI.skin.box.padding.left;
				float offsetY = ReorderableList.Defaults.padding + GUI.skin.box.padding.top -1f ;
				DrawClipPropertiesHelper.DrawPlaybackIndicator(librariesRect.Scoping(position, new Vector2(offsetX,offsetY)), -_librariesScrollPos);

				GUILayout.Space(_verticalGapDrawer.GetSpace());

				assetListRect.width -= _verticalGapDrawer.GetTotalSpace() - _verticalGapDrawer.SingleLineSpace;
				assetListRect.height -= DefaultLayoutPadding * 2;
				DrawAssetList(assetListRect);
			}		
			EditorGUILayout.EndHorizontal();
		}

		private void DrawAssetList(Rect assetListRect)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box,GUILayout.Width(assetListRect.width),GUILayout.Height(assetListRect.height));
			{
				_assetListScrollPos = EditorGUILayout.BeginScrollView(_assetListScrollPos);
				{
					_assetReorderableList.DoLayoutList();
				}
				EditorGUILayout.EndScrollView();

				if (TryGetCurrentAssetEditor(out var editor))
				{
					DrawLibraryStateMessage(editor);
				}
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
			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(librariesRect.width),GUILayout.Height(librariesRect.height));
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

		// The ReorderableList default header background GUIStyle has set fixedHeight to non-0 and stretchHeight to false, which is unreasonable...
		// Use another style or Draw it manually could solve the problem and accept more customization.
		private void DrawLibraryHeader(IAudioAsset asset)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(EditorGUIUtility.IconContent(IconConstant.BackButton), GUILayout.Width(BackButtonSize), GUILayout.Height(BackButtonSize)))
				{
					_assetReorderableList.index = -1;
					// TODO: quit temp asset
				}
				GUILayout.Space(10f);

				Rect headerRect = GUILayoutUtility.GetRect(_librariesHeaderSize.x, _librariesHeaderSize.y);

				bool hasAssetName = !string.IsNullOrEmpty(asset.AssetName);
				ToggleTempGuidingFlash(hasAssetName);
				if (!hasAssetName)
				{
					DrawUnnamedReminder(headerRect);
					headerRect.size -= Vector2.one * 4f;
					headerRect.position += Vector2.one * 2f;
				}

				if (Event.current.type == EventType.Repaint)
				{
					GUIStyle header = new GUIStyle(GUI.skin.window);
					header.Draw(headerRect, false, false, false, false);
				}

				DrawAssetNameField(headerRect, asset);

				GUILayout.FlexibleSpace();

				Rect audioTypeRect = GUILayoutUtility.GetRect(_headerAudioTypeSize.x, _headerAudioTypeSize.y);
				audioTypeRect.y += _librariesHeaderSize.y - audioTypeRect.height;
				GUIContent audioTypeGUI = new GUIContent(asset.AudioType.ToString(), "Click to change audio type");
				if (GUI.Button(audioTypeRect, audioTypeGUI))
				{
					_changeAudioTypeOption.DropDown(audioTypeRect);
				}
				EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(asset.AudioType));
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawAssetNameField(Rect headerRect, IAudioAsset asset)
		{
			string namingHint = _instruction.GetText(Instruction.LibraryManager_NameTempAssetHint);

			string displayName = string.IsNullOrWhiteSpace(asset.AssetName) ? namingHint : asset.AssetName;

			GUIStyle wordWrapStyle = new GUIStyle(GUIStyleHelper.MiddleCenterRichText);
			wordWrapStyle.wordWrap = true;
			wordWrapStyle.fontSize = AssetNameFontSize;

			EditorGUI.BeginChangeCheck();
			string newName = EditorGUI.TextField(headerRect, displayName, wordWrapStyle);
			if (EditorGUI.EndChangeCheck() && newName != asset.AssetName && !Utility.IsInvalidName(newName, out Utility.ValidationErrorCode code))
			{
				// todo: invalid的提示不夠明顯
				asset.AssetName = newName;
			}
		}
	}
}