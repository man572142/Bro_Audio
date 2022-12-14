using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MiProduction.BroAudio.Utility;
using System;
using System.Linq;
using MiProduction.BroAudio.Library.Core;

namespace MiProduction.BroAudio
{
	public class BroAudioEditorWindow : EditorWindow
	{
		public static readonly Vector2 MinWindowSize = new Vector2(960f, 540f);

		internal GUIStyleHelper GUIStyle = GUIStyleHelper.Instance;

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
				// ??AssetGUID
				throw new NotImplementedException();
			}

		}
		private void OnCreateLibrary(AudioType audioType)
		{
			BroAudioDirectory newAssetPath = new BroAudioDirectory(RootDir, "test_" + audioType.ToString() + ".asset");
			var newAsset = ScriptableObject.CreateInstance(audioType.GetLibraryTypeName());
			AssetDatabase.CreateAsset(newAsset, newAssetPath.FilePath);


			AssetDatabase.SaveAssets();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("BroAudio".ToBold().SetColor(Color.yellow).SetSize(30), GUIStyle.RichText);
			EditorGUILayout.Space(20f);
			EnumsDir.Path = DrawPathSetting("Enums Path :", EnumsDir);
			JsonFileDir.Path = DrawPathSetting("Json Path :", JsonFileDir);

			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.Space(10f);
				DrawLibraryAssetList(position.width * 0.3f -10f);
				EditorGUILayout.Space(10f);
				DrawLibrarySetting(position.width * 0.7f - 30f);
			}
			EditorGUILayout.EndHorizontal();
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

		private string DrawPathSetting(string name,BroAudioDirectory dir)
		{
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(name, GUILayout.Width(90f)))
				{
					string newPath = EditorUtility.OpenFolderPanel("", dir.Path, "");
					dir.Path = string.IsNullOrEmpty(newPath) ? dir.Path : "Assets" + newPath.Replace(Application.dataPath, string.Empty);
				}
				EditorGUILayout.LabelField(dir.Path);
			}
			EditorGUILayout.EndHorizontal();
			return dir.Path;
		}
	}

}