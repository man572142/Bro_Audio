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

			InitLibraryOptionGenericMenu();
			InitReorderableList();
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
					_libraryOption.AddItem(new GUIContent(audioType.ToString()), false, () => OnCreateLibrary(audioType));
				}
			});
		}
		
		private void InitReorderableList()
		{
			string[] libraryNameList = _datas.Select(x => x.LibraryName).Distinct().ToArray();

			_libraryList = new ReorderableList(libraryNameList, typeof(AudioData));
			_libraryList.onAddDropdownCallback = OnAddDropdown;
			_libraryList.onRemoveCallback = OnRemove;
			
			void OnAddDropdown(Rect buttonRect, ReorderableList list)
			{
				_libraryOption.DropDown(buttonRect);
			}

			void OnRemove(ReorderableList list)
			{
				// 用AssetGUID
				throw new NotImplementedException();
			}

		}
		private void OnCreateLibrary(AudioType audioType)
		{
			// 先用假檔名測試
			string fileName = "test_" + audioType.ToString() + ".asset";
			var newAsset = ScriptableObject.CreateInstance(audioType.GetLibraryTypeName());
			AssetDatabase.CreateAsset(newAsset, GetFilePath(RootPath, fileName));


			AssetDatabase.SaveAssets();
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
					int selectedIndex = _libraryList.index > 0 ? _libraryList.index : 0;
					string assetPath = AssetDatabase.GUIDToAssetPath(_datas[selectedIndex].AssetGUID);
					Editor editor = Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)));
					if (TryGetAudioLibraryEditor(editor, out var libraryEditor))
					{
						libraryEditor.IsInEditorWindow = true;
						DrawLibraryRefreshHelper(libraryEditor);
					}

					EditorGUILayout.Space(20f);
					_settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos);
					{
						editor.OnInspectorGUI();
					}
					EditorGUILayout.EndScrollView();
				}
				else
				{
					EditorGUILayout.LabelField("No Data!".SetSize(50).SetColor(Color.gray), GUIStyle.RichText);
				}
				
			}
			EditorGUILayout.EndVertical();

			bool TryGetAudioLibraryEditor(Editor libraryEditor,out AudioLibraryAssetEditor result)
			{
				result = libraryEditor as AudioLibraryAssetEditor;
				return result != null;
			}
		}

		private static void DrawLibraryRefreshHelper(AudioLibraryAssetEditor libraryEditor)
		{
			if (libraryEditor.IsLibraryNeedRefresh())
			{
				EditorGUILayout.HelpBox("This library needs to be updated !", MessageType.Warning);
				if (GUILayout.Button("Update", GUILayout.Height(30f)))
				{
					libraryEditor.UpdateLibrary();
				}
			}
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