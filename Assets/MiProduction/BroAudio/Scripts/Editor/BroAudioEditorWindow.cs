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

		private List<AudioData> _datas = null;
		private ReorderableList _libraryList = null;
		private GenericMenu _libraryOption = null;

		private Dictionary<string, Editor> _libraryEditorDict = new Dictionary<string, Editor>();
		private List<string> _libraryNameList = null;
		private string _currentSelectedLibrary = string.Empty;

		private Vector2 _libraryListScrollPos = Vector2.zero;
		private Vector2 _settingScrollPos = Vector2.zero;

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
			_datas = ReadJson();
			if(_datas != null)
			{
				_libraryNameList = _datas.Select(x => x.LibraryName).Distinct().ToList();
			}

			InitEditorDictionary();
			InitLibraryOptionGenericMenu();
			InitReorderableList();
		}

		private void OnDisable()
		{
			foreach(Editor editor in _libraryEditorDict.Values)
			{
				DestroyImmediate(editor);
			}
		}


		private void InitEditorDictionary()
		{
			_libraryEditorDict.Clear();
			foreach(AudioData data in _datas)
			{
				if(!_libraryEditorDict.ContainsKey(data.LibraryName))
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(data.AssetGUID);
					_libraryEditorDict.Add(data.LibraryName, CreateLibraryEditor(data.LibraryName, assetPath));
				}
			}
		}

		private Editor CreateLibraryEditor(string libraryName, string assetPath)
		{
			Editor editor = Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)));

			AudioLibraryAssetEditor libraryEditor = editor as AudioLibraryAssetEditor;
			if (libraryEditor != null)
			{
				libraryEditor.LibraryName = libraryName;
				libraryEditor.serializedObject.ApplyModifiedProperties();
			}

			return editor;
		}
		private void InitReorderableList()
		{
			_libraryList = new ReorderableList(_libraryNameList, typeof(AudioData));
			_libraryList.onAddDropdownCallback = OnAddDropdown;
			_libraryList.onRemoveCallback = OnRemove;

			void OnAddDropdown(Rect buttonRect, ReorderableList list)
			{
				_libraryOption.DropDown(buttonRect);
			}

			void OnRemove(ReorderableList list)
			{
				// ¥ÎAssetGUID

			}
		}

		private void InitLibraryOptionGenericMenu()
		{
			_libraryOption = new GenericMenu();

			LoopAllAudioType((AudioType audioType) => 
			{ 
				if(audioType == AudioType.None)
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

		private void OnCreateLibraryAskName(AudioType audioType)
		{
			LibraryNameEditorWindow.ShowWindow(_libraryNameList,(libraryName)=> OnCreateLibrary(libraryName,audioType));
		}

		private void OnCreateLibrary(string libraryName, AudioType audioType)
		{
			string fileName = libraryName + ".asset";
			string path = GetFilePath(RootPath, fileName);

			var newAsset = ScriptableObject.CreateInstance(audioType.GetLibraryTypeName());
			AssetDatabase.CreateAsset(newAsset, path);
			AssetDatabase.SaveAssets();

			string guid = AssetDatabase.AssetPathToGUID(path);

			_libraryNameList.Add(libraryName);
			_libraryEditorDict.Add(libraryName, CreateLibraryEditor(libraryName, path));

			CreateNewLibrary(guid, libraryName, _datas);
		}


		private void OnGUI()
		{
			EditorGUILayout.LabelField("BroAudio".ToBold().SetColor(Color.yellow).SetSize(30), GUIStyle.RichText);
			EditorGUILayout.Space(20f);

			RootPath = DrawPathSetting("Root Path :", RootPath);
			if(!IsValidRootPath())
			{
				return;
			}
			EnumsPath = DrawPathSetting("Enums Path :", EnumsPath);

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.Space(10f);
				DrawLibraryAssetList(position.width * 0.3f -10f);
				EditorGUILayout.Space(10f);
				DrawLibrarySetting(position.width * 0.7f - 30f);
			}
			EditorGUILayout.EndHorizontal();
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

		private void DrawLibraryAssetList(float width)
		{
			EditorGUILayout.BeginVertical(GUIStyle.DefaultDarkBackground,GUILayout.Width(width));
			{
				EditorGUILayout.LabelField("Library".SetColor(Color.white).ToBold(),GUIStyle.RichText);
				_libraryListScrollPos = EditorGUILayout.BeginScrollView(_libraryListScrollPos);
				{
					_libraryList.DoLayoutList();
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
		}

		private void DrawLibrarySetting(float width)
		{
			EditorGUILayout.BeginVertical(GUIStyle.DefaultDarkBackground,GUILayout.Width(width));
			{
				EditorGUILayout.LabelField("Setting".SetColor(Color.white).ToBold(), GUIStyle.RichText);

				if (_libraryList.count > 0)
				{
					_settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos);
					{
						int selectedIndex = _libraryList.index > 0 ? _libraryList.index : 0;
						string libraryName = _libraryNameList[selectedIndex];
						if (_libraryEditorDict.TryGetValue(libraryName,out Editor editor))
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
			EditorGUILayout.EndVertical();
		}

		private string DrawPathSetting(string buttonName,string path)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(buttonName, GUILayout.Width(90f)))
				{
					string newPath = EditorUtility.OpenFolderPanel("", path, "");
					if (!string.IsNullOrEmpty(newPath))
					{
						path = newPath.Substring(UnityAssetsRootPath.Length + 1);
					}
				}
				EditorGUILayout.LabelField(path);
			}
			EditorGUILayout.EndHorizontal();
			return path;
		}
	}

}