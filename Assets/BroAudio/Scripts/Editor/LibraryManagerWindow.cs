using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using Ami.Extension;
using Ami.BroAudio.Editor.Setting;
using static Ami.BroAudio.Utility;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

namespace Ami.BroAudio.Editor
{
	public class LibraryManagerWindow : EditorWindow
	{
		private class VerticalGapDrawingHelper : IEditorDrawLineCounter
		{
			public float SingleLineSpace => 10f;
			public int DrawLineCount { get; set; }
			public float GetTotalSpace() => DrawLineCount * SingleLineSpace;

			public float GetSpace()
			{
				DrawLineCount++;
				return SingleLineSpace;
			}
		}

		public event Action OnCloseLibraryManagerWindow;
		public event Action OnSelectAsset;

		private List<string> _allAssetGUIDs = null;
		private ReorderableList _assetReorderableList = null;
		private int _currSelectedAssetIndex = -1;
		private GenericMenu _assetOption = null;

		private Dictionary<string, AudioAssetEditor> _assetEditorDict = new Dictionary<string, AudioAssetEditor>();

		private Vector2 _assetListScrollPos = Vector2.zero;
		private Vector2 _settingScrollPos = Vector2.zero;

		private GUIStyle _assetNameTitleStyle = null;
		private VerticalGapDrawingHelper _gapDrawer = new VerticalGapDrawingHelper();
		private LibraryIDController _libraryIdController = new LibraryIDController();

		private Gradient gradient = new Gradient();


		[MenuItem(LibraryManagerMenuPath, false,LibraryManagerMenuIndex)]
		public static LibraryManagerWindow ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(LibraryManagerWindow));
			window.minSize = BroAudioGUISetting.MinWindowSize;
			window.titleContent = new GUIContent("Library Manager");
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

		private void OnLostFocus()
		{
			EditorPlayAudioClip.StopAllClips();
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

		private void OnEnable()
		{
			_allAssetGUIDs = GetGUIDListFromJson();

			InitGUIStyle();

			InitEditorDictionary();
			InitAssetOptionGenericMenu();
			InitReorderableList();
		}


		private void OnDisable()
		{
			OnCloseLibraryManagerWindow?.Invoke();
			foreach (AudioAssetEditor editor in _assetEditorDict.Values)
			{
				DestroyImmediate(editor);
			}	
		}

		#region Init
		private void InitGUIStyle()
		{
			_assetNameTitleStyle = GUIStyleHelper.Instance.UpperCenterStyle;
		}

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

		private AudioAssetEditor CreateAssetEditor(string guid , string assetName = "")
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			AudioAssetEditor editor = UnityEditor.Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject))) as AudioAssetEditor;
			if(string.IsNullOrEmpty(editor.Asset.AssetName))
			{
				string assetNamePropertyPath = EditorScriptingExtension.GetAutoBackingFieldName(nameof(Data.IAudioAsset.AssetName));
				editor.serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

				string assetGUIDPropertyPath = EditorScriptingExtension.GetFieldName(nameof(Data.IAudioAsset.AssetGUID));
				editor.serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

				editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			_libraryIdController.AddByAsset(editor.Asset);
			editor.SetIDAccessor(_libraryIdController);
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
				_assetOption.DropDown(buttonRect);
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

					EditorGUI.DrawRect(audioTypeRect, GlobalSetting.GetAudioTypeColor(editor.Asset.AudioType));
					EditorGUI.LabelField(audioTypeRect, editor.Asset.AudioType.ToString(), GUIStyleHelper.Instance.MiddleCenterText);
				}
			}
		}

		private void OnSelect(ReorderableList list)
		{
			if (list.index != _currSelectedAssetIndex)
			{
				OnSelectAsset?.Invoke();
				_currSelectedAssetIndex = list.index;
			}
		}

		private void InitAssetOptionGenericMenu()
		{
			_assetOption = new GenericMenu();

			ForeachAudioType((BroAudioType audioType) =>
			{
				if (audioType == BroAudioType.None)
				{
					_assetOption.AddItem(new GUIContent("Choose an AudioType to create an asset"), false, null);
					_assetOption.AddSeparator("");
				}
				else
				{
					_assetOption.AddItem(new GUIContent(audioType.ToString()), false, () => OnCreateAssetAskName(audioType));
				}
			});
		} 
		#endregion

		private void OnCreateAssetAskName(BroAudioType audioType)
		{
			// In the following case. List has better performance than IEnumerable , even with a ToList() method.
			List<string> assetNames = _assetEditorDict.Values.Select(x => x.Asset.AssetName).ToList();
			AssetNameEditorWindow.ShowWindow(assetNames, (assetName)=> OnCreateAsset(assetName,audioType));
		}

		private void OnCreateAsset(string libraryName, BroAudioType audioType)
		{
			if(string.IsNullOrEmpty(AssetOutputPath))
			{
				return;
			}

			string fileName = libraryName + ".asset";
			string path = GetFilePath(AssetOutputPath, fileName);

			var newAsset = ScriptableObject.CreateInstance(audioType.GetAssetType());
			AssetDatabase.CreateAsset(newAsset, path);
			AddToSoundManager(newAsset);
			AssetDatabase.SaveAssets();

			string guid = AssetDatabase.AssetPathToGUID(path);

			_allAssetGUIDs.Add(guid);
			_assetEditorDict.Add(guid, CreateAssetEditor(guid, libraryName));

			WriteGuidToCoreData(_allAssetGUIDs);	
		}

		private bool TryGetCurrentAssetEditor(out AudioAssetEditor editor)
		{
			editor = null;
			if (_allAssetGUIDs.Count > 0)
			{
				int index = Mathf.Clamp(_assetReorderableList.index, 0, _allAssetGUIDs.Count - 1);
				if (_assetEditorDict.TryGetValue(_allAssetGUIDs[index], out editor))
				{
					return true;
				}
			}
			return false;
		}

		private void OnGUI()
		{
			_gapDrawer.DrawLineCount = 0;

			//DrawASCIITitle();
			

			EditorGUILayout.BeginHorizontal();
			{                
                EditorGUILayout.Space(_gapDrawer.GetSpace());
				EditorScriptingExtension.SplitRectHorizontal(position, 0.3f, _gapDrawer.GetSpace(), out Rect assetListRect, out Rect librariesRect);

				EditorGUILayout.BeginVertical();
				{
                    EditorGUILayout.LabelField(nameof(BroAudio).ToBold().SetColor(MainTitleColor).SetSize(30), GUIStyleHelper.Instance.RichText);
                    EditorGUILayout.Space(10f);
                    DrawAssetList(assetListRect);
                }
				EditorGUILayout.EndVertical();
				
				EditorGUILayout.Space(_gapDrawer.GetSpace());
				librariesRect.width -= _gapDrawer.GetTotalSpace();
				DrawLibrariesList(librariesRect);
			}
			EditorGUILayout.EndHorizontal();
		}

		

		private void DrawAssetList(Rect rect)
		{
			EditorGUILayout.BeginVertical(GUIStyleHelper.Instance.DefaultDarkBackground,GUILayout.Width(rect.width));
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
				switch (state)
				{
					case LibraryState.HasEmptyName:
						EditorScriptingExtension.RichTextHelpBox($"There are some empty name in asset: {assetName}!", MessageType.Error);
						break;
					case LibraryState.HasDuplicateName:
						EditorScriptingExtension.RichTextHelpBox($"Name:{dataName} is duplicated in asset: {assetName} !", MessageType.Error);
						break;
					case LibraryState.HasInvalidName:
						EditorScriptingExtension.RichTextHelpBox($"Name:{dataName} in asset: {assetName} has invalid word !", MessageType.Error);
						break;
					case LibraryState.Fine:
						EditorScriptingExtension.RichTextHelpBox($"Everything works great!", "Toggle Icon");
						break;
				}
			}
		}

		private void DrawLibrariesList(Rect rect)
		{
			EditorGUILayout.BeginVertical(GUIStyleHelper.Instance.DefaultDarkBackground,GUILayout.Width(rect.width));
			{
				if (TryGetCurrentAssetEditor(out var editor))
				{
					EditorGUILayout.LabelField(editor.Asset.AssetName.SetSize(25).SetColor(Color.white), _assetNameTitleStyle);
					EditorGUILayout.Space(10f);
					if (_assetReorderableList.count > 0)
					{
						_settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos);
						{
							editor.DrawLibraries();
						}
						EditorGUILayout.EndScrollView();
					}
					else
					{
						EditorGUILayout.LabelField("No Libraries!".SetSize(50).SetColor(Color.gray), GUIStyleHelper.Instance.RichText);
					}
				}
				else
				{
					EditorGUILayout.LabelField("No Asset".SetSize(30).SetColor(Color.white), GUIStyleHelper.Instance.RichText);
				}
				
			}
			EditorGUILayout.EndVertical();
		}

        private void DrawASCIITitle()
        {
			
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.font = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
			style.richText = true;

			string ascii =
							"########  ########   #######     ###    ##     ## ########  ####  #######  " +
							"##     ## ##     ## ##     ##   ## ##   ##     ## ##     ##  ##  ##     ## " +
							"##     ## ##     ## ##     ##  ##   ##  ##     ## ##     ##  ##  ##     ## " +
							"########  ########  ##     ## ##     ## ##     ## ##     ##  ##  ##     ## " +
							"##     ## ##   ##   ##     ## ######### ##     ## ##     ##  ##  ##     ## " +
							"##     ## ##    ##  ##     ## ##     ## ##     ## ##     ##  ##  ##     ## " +
							"########  ##     ##  #######  ##     ##  #######  ########  ####  #######  ";

			int oneLineLength = "########  ########   #######     ###    ##     ## ########  ####  #######  ".Length;

			int lineCount = 5;
			int currentLine = 0;

			string line = string.Empty;

			for (int i = 0; i < ascii.Length; i++)
			{
				if(ascii[i] == ' ')
				{
					line += "_";
                }
				else
				{
                    line += ascii[i];
                }
				
				if (i != 0 && (i + 1) % oneLineLength == 0)
				{
					float evalute = currentLine / (float)lineCount;
					Color color = gradient.Evaluate(evalute);
					EditorGUILayout.LabelField(line.SetColor(color), style, GUILayout.Height(EditorGUIUtility.singleLineHeight *0.5f));
					line = string.Empty;
					currentLine++;
				}
			}
			gradient = EditorGUILayout.GradientField(gradient, GUILayout.Width(500f));
		}
    }
}