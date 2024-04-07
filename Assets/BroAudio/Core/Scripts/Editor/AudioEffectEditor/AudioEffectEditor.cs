using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Runtime;
using System.Linq;
using UnityEngine.Audio;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

namespace Ami.BroAudio.Editor
{
	public class AudioEffectEditor : EditorWindow
	{
		private AudioMixer _mixer = null;
		private UnityEditor.Editor _effectTrackEditor = null; 
		private Vector2 _scrollPos = default;
		private BroInstructionHelper _instruction = new BroInstructionHelper();

		private EditorSetting Setting => BroEditorUtility.EditorSetting;

		[MenuItem(AudioEffectMenuPath, false, AudioEffectEditorMenuIndex)]
		public static void ShowWindow()
		{
			EditorWindow window = GetWindow<AudioEffectEditor>();
			window.minSize = new Vector2(500f, 300f);
			window.titleContent = new GUIContent(MenuItem_EffectEditor, EditorGUIUtility.IconContent(IconConstant.AudioGroup).image);
			window.Show();
		}

		private void OnEnable()
		{
			SoundManager manager = Resources.Load<SoundManager>(nameof(SoundManager));
			if(manager && manager.Mixer)
			{
				_mixer = manager.Mixer;
				var effectTrack = _mixer.FindMatchingGroups(EffectTrackName).FirstOrDefault();
				if(effectTrack)
				{
					_effectTrackEditor = UnityEditor.Editor.CreateEditor(effectTrack);
				}
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorGUIUtility.singleLineHeight));
			{
				GUILayout.FlexibleSpace();

				var buttonContent = new GUIContent("Effect Exposed Parameters");
				if (EditorGUILayout.DropdownButton(buttonContent, FocusType.Passive))
				{
					var exposedParaPopup = new CustomExposedParametersPopupWindow();
					exposedParaPopup.CreateReorderableList(_mixer);
					Rect rect = new Rect(new Vector2(position.width, 0f), Vector2.zero);
					PopupWindow.Show(rect, exposedParaPopup);
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
			{
				_effectTrackEditor.OnInspectorGUI();
			}
			EditorGUILayout.EndScrollView();
		}
	}
}