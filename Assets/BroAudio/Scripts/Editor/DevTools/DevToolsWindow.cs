using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Linq;
using static Ami.BroAudio.BroLog;
using static Ami.BroAudio.BroName;

namespace Ami.Extension
{
	public class DevToolsWindow : EditorWindow
	{
		public const string DialogTitle = "Confirm";
		public const string DialogMessage = "This is advanced function [{0}],Are you sure you want to execute?";
		public const string Confirm = "Yes";
		public const string Cancel = "No";

		public const string Function_CreateNewAudioMixerGroup = "Create New BroAuio Track";

		private AudioMixer _targetMixer = null;

		//[MenuItem("BroAudio/Dev Only")]
		public static void ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(DevToolsWindow));
			window.minSize = new Vector2(640f, 360f);
			window.titleContent = new GUIContent("Refelection Tool");
			window.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();
			_targetMixer = EditorGUILayout.ObjectField("AudioMixer", _targetMixer, typeof(AudioMixer), false) as AudioMixer;

			if (!_targetMixer)
			{
				return;
			}
			for (int i = 0; i < 3; i++)
			{
				EditorGUILayout.Space(10);
			}

			var buttonHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f);
			DuplicateOneTrackAndCopySetting(buttonHeight);

			EditorGUILayout.Space();
		}

		private void DuplicateOneTrackAndCopySetting(GUILayoutOption buttonHeight)
		{
			if (GUILayout.Button(Function_CreateNewAudioMixerGroup, buttonHeight) && DisplayDialog(Function_CreateNewAudioMixerGroup))
			{
				AudioMixerGroup mainTrack = _targetMixer.FindMatchingGroups(MainTrackName)?.FirstOrDefault();
				AudioMixerGroup[] tracks = _targetMixer.FindMatchingGroups(GenericTrackName);
				int tracksCount = tracks.Length;
				if (mainTrack == default || tracks == default)
				{
					LogError($"Can't get the Main track or other BroAudio track");
					return;
				}

				string trackName = $"{GenericTrackName}{tracksCount + 1}";
				BroAudioReflection.DuplicateBroAudioTrack(_targetMixer,mainTrack,tracks.Last(), trackName);
			}
		}


		private bool DisplayDialog(string functionName)
		{
			return EditorUtility.DisplayDialog(DialogTitle, string.Format(DialogMessage, functionName), Confirm, Cancel);
		}
	}
}