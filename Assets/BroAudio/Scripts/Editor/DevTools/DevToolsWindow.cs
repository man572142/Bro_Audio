using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Linq;

namespace Ami.Extension
{
	public class DevToolsWindow : EditorWindow
	{
		public const string DialogTitle = "Confirm";
		public const string DialogMessage = "This is advanced function [{0}],Are you sure you want to execute?";
		public const string Confirm = "Yes";
		public const string Cancel = "No";

		public const string Function_CreateNewAudioMixerGroup = "Create New Audio Mixer Group with BroAuio Settings";
		public const string Function_ExposeSendLevel = "Expose All Send Effect Mix Level";
		public const string Function_EnableSendWetMix = "Enable All Send Wet Mix";
		public const string Function_SetSendLevel = "Set All Send Wet Mix Level";


		private AudioMixer _targetMixer = null;

		[MenuItem("BroAudio/Dev Only")]
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
			DuplicateTrackAndCopySetting(buttonHeight);

			EditorGUILayout.Space();

			if(GUILayout.Button("Test", buttonHeight))
			{
				
			}
		}

		private void DuplicateTrackAndCopySetting(GUILayoutOption buttonHeight)
		{
			if (GUILayout.Button(Function_CreateNewAudioMixerGroup, buttonHeight) && DisplayDialog(Function_CreateNewAudioMixerGroup))
			{
				 _targetMixer.FindMatchingGroups(BroAudioReflection.GenericTrackName);

				var newGroup = BroAudioReflection.DuplicateBroAudioMixerGroup(_targetMixer);
			}
		}

		private bool DisplayDialog(string functionName)
		{
			return EditorUtility.DisplayDialog(DialogTitle, string.Format(DialogMessage, functionName), Confirm, Cancel);
		}

	}

}