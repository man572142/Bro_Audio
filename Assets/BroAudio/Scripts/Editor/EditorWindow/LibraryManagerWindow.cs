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
using static Ami.Extension.EditorScriptingExtension;
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

		public static event Action OnCloseLibraryManagerWindow;
		public static event Action OnSelectAsset;

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
					editor.Init();
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
					string displayName = string.IsNullOrEmpty(editor.Asset.AssetName)? "Temp".SetColor(FalseColor) : editor.Asset.AssetName.SetColor(UnityDefaultEditorColor);
					EditorGUI.LabelField(labelRect, displayName,GUIStyleHelper.RichText);

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
				foreach(var pair in _assetEditorDict)
				{
					string guid = pair.Key;
					var editor = pair.Value;
					if(guid == _allAssetGUIDs[list.index])
					{
						editor.AddEntitiesNameChangeListener();
						editor.Verify();
					}
					else
					{
						editor.RemoveEntitiesNameChangeListener();
					}
				}
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
				// TODO: ���F������̭���ID�٬O�ª�AudioType
			}
		}
		#endregion

		private void ShowCreateAssetAskName(object type)
        {
			// In the following case. List has better performance than IEnumerable , even with a ToList() method.
			List<string> assetNames = _assetEditorDict.Values.Select(x => x.Asset.AssetName).ToList();
			AssetNameEditorWindow.ShowWindow(assetNames, (assetName) => CreateAsset(assetName, (BroAudioType)type));
        }

        private AudioAssetEditor CreateAsset(string libraryName, BroAudioType audioType)
		{
			if(!TryGetNewPath(libraryName,out string path,out string fileName))
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
			editor.Init();
			editor.SetData(guid, fileName, audioType);
			
			_assetEditorDict.Add(guid, editor);
			_allAssetGUIDs.Add(guid);

			WriteGuidToCoreData(_allAssetGUIDs);
			_assetReorderableList.index = _assetReorderableList.count - 1;
			return editor;
		}

		private bool TryGetNewPath(string libraryName,out string path,out string fileName)
		{
			path = string.Empty;
			fileName = libraryName;
			if (!string.IsNullOrEmpty(AssetOutputPath))
			{
				int index = 0;
				path = GetNewAssetPath(libraryName);
				while(File.Exists(path))
				{
					index++;
					fileName = libraryName + index.ToString();
					path = GetNewAssetPath(fileName);
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
			editor = null;
			if (_allAssetGUIDs == null || _assetReorderableList == null)
			{
				return false;
			}

			if (_allAssetGUIDs.Count > 0 && _assetReorderableList.index >= 0)
			{
				int index = Mathf.Clamp(_assetReorderableList.index, 0, _allAssetGUIDs.Count - 1);
				if (_assetEditorDict.TryGetValue(_allAssetGUIDs[index], out editor))
				{
					return true;
				}
			}
			else if(_assetEditorDict.TryGetValue(BroName.TempAssetName, out editor))
			{
				return true;
			}
			return false;
		}

		private void OnAssetNameChanged(AudioAssetEditor editor, string newName)
		{
			editor.serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AssetName))).stringValue = newName;
			editor.serializedObject.ApplyModifiedProperties();
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
					DrawIssueMessage(editor);
				}
			}
			EditorGUILayout.EndVertical();

			void DrawIssueMessage(AudioAssetEditor assetEditor)
			{
				string assetName = assetEditor.Asset.AssetName.ToBold().SetColor(Color.white);
				string dataName = assetEditor.IssueEntityName;
				string text = _instruction.GetText(assetEditor.CurrInstruction); ;
				
				switch (assetEditor.CurrInstruction)
				{
					case Instruction.AssetNaming_StartWithTemp:
					case Instruction.EntityIssue_HasEmptyName:
						EditorScriptingExtension.RichTextHelpBox(String.Format(text, assetName), MessageType.Error);
						break;
					case Instruction.EntityIssue_IsDuplicated:
					case Instruction.EntityIssue_ContainsInvalidWords:
						EditorScriptingExtension.RichTextHelpBox(String.Format(text, dataName, assetName), MessageType.Error);
						break;
					case Instruction.None:
						EditorScriptingExtension.RichTextHelpBox(text, IconConstant.LibraryWorksFine);
						break;
					default:
						EditorGUILayout.HelpBox(text, MessageType.Error);
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
						DrawLibraryHeader(editor.Asset, newName => OnAssetNameChanged(editor,newName));
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
		private void DrawLibraryHeader(IAudioAsset asset,Action<string> onAssetNameChanged)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(EditorGUIUtility.IconContent(IconConstant.BackButton), GUILayout.Width(BackButtonSize), GUILayout.Height(BackButtonSize)))
				{
					_assetReorderableList.index = -1;
				}
				GUILayout.Space(10f);

				Rect headerRect = GUILayoutUtility.GetRect(_librariesHeaderSize.x, _librariesHeaderSize.y);

				bool hasAssetName = !string.IsNullOrEmpty(asset.AssetName);
				ToggleTempGuidingFlash(hasAssetName);
				if (!hasAssetName)
				{
					DrawFlashingReminder(headerRect);
					headerRect.size -= Vector2.one * 4f;
					headerRect.position += Vector2.one * 2f;
				}

				if (Event.current.type == EventType.Repaint)
				{
					GUIStyle header = new GUIStyle(GUI.skin.window);
					header.Draw(headerRect, false, false, false, false);
				}

				DrawAssetNameField(headerRect, asset, onAssetNameChanged);

				GUILayout.FlexibleSpace();

				Rect audioTypeRect = GUILayoutUtility.GetRect(_headerAudioTypeSize.x, _headerAudioTypeSize.y);
				audioTypeRect.y += _librariesHeaderSize.y - audioTypeRect.height;
				GUIContent audioTypeGUI = new GUIContent(asset.AudioType.ToString(), "Click to change audio type");

				if (asset.AudioType == BroAudioType.None)
				{
					DrawFlashingReminder(audioTypeRect);
					audioTypeRect.size -= Vector2.one * 4f;
					audioTypeRect.position += Vector2.one * 2f;
				}
				
				if (GUI.Button(audioTypeRect, audioTypeGUI))
				{
					_changeAudioTypeOption.DropDown(audioTypeRect);
				}
				EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(asset.AudioType));
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawAssetNameField(Rect headerRect, IAudioAsset asset, Action<string> onAssetNameChanged)
		{
			string namingHint = _instruction.GetText(Instruction.LibraryManager_NameTempAssetHint);

			string displayName = string.IsNullOrWhiteSpace(asset.AssetName) ? namingHint : asset.AssetName;

			GUIStyle wordWrapStyle = new GUIStyle(GUIStyleHelper.MiddleCenterRichText);
			wordWrapStyle.wordWrap = true;
			wordWrapStyle.fontSize = AssetNameFontSize;

			EditorGUI.BeginChangeCheck();
			string newName = EditorGUI.DelayedTextField(headerRect, displayName, wordWrapStyle);
			if (EditorGUI.EndChangeCheck() && newName != asset.AssetName && IsValidAssetName(newName))
			{
				string path = AssetDatabase.GetAssetPath(asset as AudioAsset);
				string result = AssetDatabase.RenameAsset(path, newName);
				if (string.IsNullOrEmpty(result))
				{
					onAssetNameChanged?.Invoke(newName);
				}
			}
		}

		private bool IsValidAssetName(string newName)
		{
			if (IsInvalidName(newName, out ValidationErrorCode code))
			{
				switch (code)
				{
					case ValidationErrorCode.StartWithNumber:
						ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_StartWithNumber)), 2f);
						break;
					case ValidationErrorCode.ContainsInvalidWord:
						ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_ContainsInvalidWords)), 2f);
						break;
					case ValidationErrorCode.ContainsWhiteSpace:
						ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_ContainsWhiteSpace)), 2f);
						break;
					case ValidationErrorCode.IsDuplicate:
						ShowNotification(new GUIContent(_instruction.GetText(Instruction.AssetNaming_IsDuplicated)), 2f);
						break;
				}
				return false;
			}
			else if (IsTempReservedName(newName))
			{
				string text = string.Format(_instruction.GetText(Instruction.AssetNaming_StartWithTemp), newName);
				ShowNotification(new GUIContent(text), 2f);
				return false;
			}
			return true;
		}
	}
}