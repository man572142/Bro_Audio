using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MiProduction.BroAudio.Utility;
using System;

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
		}

		private void InitReorderableList()
		{
			_libraryList = new ReorderableList(_datas,typeof(AudioData));

			_libraryList.drawElementCallback = OnDrawElement;
			_libraryList.onAddDropdownCallback = OnAddDropdown;
			_libraryList.onRemoveCallback = OnRemove;
			_libraryList.onSelectCallback = OnSelect;

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				throw new NotImplementedException();
			}

			void OnSelect(ReorderableList list)
			{
				throw new NotImplementedException();
			}

			void OnRemove(ReorderableList list)
			{
				throw new NotImplementedException();
			}

			void OnAddDropdown(Rect buttonRect, ReorderableList list)
			{
				throw new NotImplementedException();
			}
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
				_settingScrollPos = EditorGUILayout.BeginScrollView(_settingScrollPos);
				{
					if (_libraryList.count > 0)
					{
						int selectedIndex = _libraryList.index > 0 ? _libraryList.index : 0;
						string assetPath = AssetDatabase.GUIDToAssetPath(_datas[selectedIndex].AssetGUID);
						Editor libraryEditor = Editor.CreateEditor(AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)));
						libraryEditor.OnInspectorGUI();
					}
				}
				EditorGUILayout.EndScrollView();
			}
			EditorGUILayout.EndVertical();
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