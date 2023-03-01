using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MiProduction.BroAudio.Utility;
using System;
using System.Linq;
using MiProduction.BroAudio.Library.Core;
using MiProduction.Extension;

namespace MiProduction.BroAudio
{
	public class BroAudioEditorWindow : EditorWindow
	{
		public static readonly Vector2 MinWindowSize = new Vector2(960f, 540f);

		public GUIStyleHelper GUIStyle = GUIStyleHelper.Instance;

		private List<string> _allLibraryGUIDs = null;
		private ReorderableList _libraryReorderableList = null;
		private GenericMenu _libraryOption = null;

		private Dictionary<string, AudioLibraryAssetEditor> _libraryEditorDict = new Dictionary<string, AudioLibraryAssetEditor>();
		//private List<string> _libraryNameList = null;

		private Vector2 _libraryListScrollPos = Vector2.zero;
		private Vector2 _settingScrollPos = Vector2.zero;

		private GUIStyle _libraryNameTitleStyle = null;

		[MenuItem("BroAudio/Library Manager")]
		public static void ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(BroAudioEditorWindow));
			window.minSize = MinWindowSize;
			window.titleContent = new GUIContent("BroAudio Library Manager");
			window.Show();
		}


		private void OnEnable()
		{
			_allLibraryGUIDs = GetGUIDListFromJson();

			InitGUIStyle();

			InitEditorDictionary();
			InitLibraryOptionGenericMenu();
			InitReorderableList();
		}


		private void OnDisable()
		{
			foreach(AudioLibraryAssetEditor editor in _libraryEditorDict.Values)
			{
				DestroyImmediate(editor);
			}
		}

		#region Init
		private void InitGUIStyle()
		{
			_libraryNameTitleStyle = GUIStyleHelper.Instance.MiddleCenterText;
			_libraryNameTitleStyle.richText = true;
		}

		private void InitEditorDictionary()
		{
			_libraryEditorDict.Clear();
			foreach (string guid in _allLibraryGUIDs)
			{
				if (!string.IsNullOrEmpty(guid) && !_libraryEditorDict.ContainsKey(guid))
				{
					_libraryEditorDict.Add(guid, CreateLibraryEditor(guid));
				}
			}
		}

		private AudioLibraryAssetEditor CreateLibraryEditor(string guid , string libraryName = "")
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			AudioLibraryAssetEditor editor = Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject))) as AudioLibraryAssetEditor;
			if(string.IsNullOrEmpty(editor.Asset.LibraryName))
			{
				editor.serializedObject.FindProperty("_libraryName").stringValue = libraryName;
				editor.serializedObject.FindProperty("_assetGUID").stringValue = guid;
				editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}
			return editor;
		}
		private void InitReorderableList()
		{
			_libraryReorderableList = new ReorderableList(_allLibraryGUIDs, typeof(string));
			_libraryReorderableList.drawHeaderCallback = OnDrawHeader;
			_libraryReorderableList.onAddDropdownCallback = OnAddDropdown;
			_libraryReorderableList.onRemoveCallback = OnRemove;
			_libraryReorderableList.drawElementCallback = OnDrawElement;

			void OnDrawHeader(Rect rect)
			{
				EditorGUI.LabelField(rect, "Library List");
			}

			void OnAddDropdown(Rect buttonRect, ReorderableList list)
			{
				_libraryOption.DropDown(buttonRect);
			}

			void OnRemove(ReorderableList list)
			{
				DeleteLibrary(_allLibraryGUIDs[list.index]);
				AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(_allLibraryGUIDs[list.index]));
				_allLibraryGUIDs.RemoveAt(list.index);
				AssetDatabase.Refresh();
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				if(_libraryEditorDict.TryGetValue(_allLibraryGUIDs[index],out var editor))
				{
					if(editor.Asset == null)
					{
						return;
					}

					Rect labelRect = rect;
					labelRect.width *= 0.7f;
					EditorGUI.LabelField(labelRect, editor.Asset.LibraryName);

					Rect audioTypeRect = rect;
					audioTypeRect.width *= 0.3f;
					audioTypeRect.x = labelRect.xMax;
					EditorGUI.DrawRect(audioTypeRect, GetAudioTypeColor(editor.Asset.AudioType));
					EditorGUI.LabelField(audioTypeRect, editor.Asset.AudioType.ToString(), GUIStyleHelper.Instance.MiddleCenterText);

					
				}
			}
		}

		private void InitLibraryOptionGenericMenu()
		{
			_libraryOption = new GenericMenu();

			LoopAllAudioType((AudioType audioType) =>
			{
				if (audioType == AudioType.None)
				{
					_libraryOption.AddItem(new GUIContent("Choose an AudioType to create a library"), false, null);
					_libraryOption.AddSeparator("");
				}
				else
				{
					_libraryOption.AddItem(new GUIContent(audioType.ToString()), false, () => OnCreateLibraryAskName(audioType));
				}
			});
		} 
		#endregion

		private void OnCreateLibraryAskName(AudioType audioType)
		{
			// In the following case. List has better performance than IEnumerable , even with a ToList() method.
			List<string> libraryNames = _libraryEditorDict.Values.Select(x => x.Asset.LibraryName).ToList();
			LibraryNameEditorWindow.ShowWindow(libraryNames, (libraryName)=> OnCreateLibrary(libraryName,audioType));
		}

		private void OnCreateLibrary(string libraryName, AudioType audioType)
		{
			string fileName = libraryName + ".asset";
			string path = GetFilePath(LibraryPath, fileName);

			var newAsset = ScriptableObject.CreateInstance(audioType.GetLibraryTypeName());
			AssetDatabase.CreateAsset(newAsset, path);
			AssetDatabase.SaveAssets();

			string guid = AssetDatabase.AssetPathToGUID(path);

			_allLibraryGUIDs.Add(guid);
			_libraryEditorDict.Add(guid, CreateLibraryEditor(guid,libraryName));

			WriteJsonToFile(_allLibraryGUIDs);
		}


		private void OnGUI()
		{
			EditorGUILayout.LabelField("BroAudio".ToBold().SetColor(Color.yellow).SetSize(30), GUIStyle.RichText);
			EditorGUILayout.Space(20f);

			//RootPath = DrawPathSetting("Root Path :", RootPath);
			//if (!IsValidRootPath())
			//{
			//	return;
			//}
			//EnumsPath = DrawPathSetting("Enums Path :", EnumsPath);
			//LibraryPath = DrawPathSetting("Library Path :", LibraryPath);

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.Space(10f);
				DrawLibraryAssetList(position.width * 0.3f - 10f);
				EditorGUILayout.Space(10f);
				DrawLibrarySetting(position.width * 0.7f - 30f, GetCurrentLibraryEditor());
			}
			EditorGUILayout.EndHorizontal();
		}

		private AudioLibraryAssetEditor GetCurrentLibraryEditor()
		{
			if (_allLibraryGUIDs.Count > 0)
			{
				int index = _libraryReorderableList.index > 0 ? _libraryReorderableList.index : 0;
				if (_libraryEditorDict.TryGetValue(_allLibraryGUIDs[index], out var editor))
				{
					return editor;
				}
			}
			return null;
		}
		

		private void DrawLibraryAssetList(float width)
		{
			EditorGUILayout.BeginVertical(GUIStyle.DefaultDarkBackground,GUILayout.Width(width));
			{
				//EditorGUILayout.LabelField("Library".SetColor(Color.white).ToBold(),GUIStyle.RichText);
				_libraryListScrollPos = EditorGUILayout.BeginScrollView(_libraryListScrollPos);
				{
					_libraryReorderableList.DoLayoutList();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawLibrarySetting(float width, AudioLibraryAssetEditor editor)
		{
			EditorGUILayout.BeginVertical(GUIStyle.DefaultDarkBackground,GUILayout.Width(width));
			{
				//EditorGUILayout.LabelField("Setting".SetColor(Color.white).ToBold(), GUIStyle.RichText);
				if(editor == null)
				{
					EditorGUILayout.LabelField("No library".SetSize(30).SetColor(Color.white), GUIStyle.RichText);
					goto DrawEnd;
				}
				EditorGUILayout.LabelField(editor.Asset.LibraryName.SetSize(25).SetColor(Color.white),_libraryNameTitleStyle);
				EditorGUILayout.Space(10f);
				DrawLibraryState(editor);
				if (_libraryReorderableList.count > 0)
				{
					_settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos);
					{
						if(editor != null)
						{
							editor.OnInspectorGUI();
						}
					}
					EditorGUILayout.EndScrollView();
				}
				else
				{
					EditorGUILayout.LabelField("No Data!".SetSize(50).SetColor(Color.gray), GUIStyle.RichText);
				}
			}
			DrawEnd:
			EditorGUILayout.EndVertical();
		}

		private bool IsValidRootPath()
		{
			string coreDataFilePath = GetFilePath(RootPath, CoreDataFileName);
			if (!System.IO.File.Exists(coreDataFilePath))
			{
				EditorGUILayout.HelpBox($"The root path should be {CoreDataFileName}'s path. Please relocate it it!\n" +
					$"If the file is missing, please click the button below to recreate it in current RootPath ", MessageType.Error);

				if (GUILayout.Button($"Recreate {CoreDataFileName}", GUILayout.Width(200f)))
				{
					CreateDefaultCoreData();
				}
				return false;
			}
			return true;
		}

		private string DrawPathSetting(string buttonName,string path)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(buttonName, GUILayout.Width(90f)))
				{
					string newPath = EditorUtility.OpenFolderPanel("", path, "");
					if (!string.IsNullOrEmpty(newPath) && IsInProjectFolder(newPath))
					{
						path = newPath.Substring(UnityAssetsRootPath.Length + 1);
					}
				}
				EditorGUILayout.LabelField(path);
			}
			EditorGUILayout.EndHorizontal();
			return path;
		}

		private void DrawLibraryState(AudioLibraryAssetEditor editor)
		{
			switch (editor.CheckLibraryState(out string dataName))
			{
				case LibraryState.Fine:
					break;
				case LibraryState.HasEmptyName:
					EditorGUILayout.HelpBox("There are some audio set's name is empty !", MessageType.Error);
					break;
				case LibraryState.HasNameDuplicated:
					
					GUIContent content = new GUIContent($"Name:{dataName.ToBold().SetColor(Color.white)} is duplicated !", EditorGUIUtility.IconContent("console.erroricon").image);
					EditorGUILayout.LabelField(content,GUIStyleHelper.Instance.RichTextHelpBox);
					break;
				case LibraryState.HasInvalidName:
					EditorGUILayout.HelpBox($"Name:[{dataName}] has invalidWord !", MessageType.Error);
					break;
				case LibraryState.NeedToUpdate:
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.HelpBox("This library needs to be updated !", MessageType.Warning);
						if (GUILayout.Button("Update", GUILayout.Height(30f)))
						{
							editor.UpdateLibrary();
						}
					}
					EditorGUILayout.EndHorizontal();
					break;
			}
		}
	}
}