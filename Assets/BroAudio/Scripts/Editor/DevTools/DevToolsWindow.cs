using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Linq;
using static Ami.BroAudio.Tools.BroLog;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.Extension.Reflection
{
	public class DevToolsWindow : EditorWindow
	{
		public const string DialogTitle = "Confirm";
		public const string DialogMessage = "This is advanced function [{0}],Are you sure you want to execute?";
		public const string Confirm = "Yes";
		public const string Cancel = "No";

		public const string Function_DuplicateLastTrack = "Duplicate The Last BroAuio Track";
		public const string Function_AddExposedParameter = "Add Exposed Parameter";

		private AudioMixer _targetMixer = null;
		private GUILayoutOption _buttonHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight* 2f);
		private ExposedParameterType _exposedParaType = ExposedParameterType.Volume;
		private AudioMixerGroup _targetExposeMixerGroup = null;
		private bool _exposeAllMixerGroup = false;

#if BroAudio_DevOnly
		[MenuItem(MenuItem_BroAudio + "Dev Only Tools")]
#endif
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
				GUILayout.Space(10);
			}

			DrawDuplicateLastTrackButton();
			EditorGUILayout.Space();
			DrawAddExposedParameterButton();
		}

		private void DrawDuplicateLastTrackButton()
		{
			if (GUILayout.Button(Function_DuplicateLastTrack, _buttonHeight) && DisplayDialog(Function_DuplicateLastTrack))
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

		private void DrawAddExposedParameterButton()
		{
			EditorGUILayout.LabelField("Expose Mixer Group Parameter",EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			{
				EditorGUILayout.BeginVertical();
				{
					_exposeAllMixerGroup = EditorGUILayout.ToggleLeft("Expose All",_exposeAllMixerGroup);
					using (var disableScope = new EditorGUI.DisabledScope(_exposeAllMixerGroup))
					{
						EditorGUILayout.LabelField("Expose Target");
						_targetExposeMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField(_targetExposeMixerGroup, typeof(AudioMixerGroup), true);
					}
				}
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical();
				{
					_exposedParaType = (ExposedParameterType)EditorGUILayout.EnumPopup("Parameter Type", _exposedParaType);

					if (GUILayout.Button(Function_AddExposedParameter, _buttonHeight) && DisplayDialog(Function_AddExposedParameter))
					{
						if(_exposeAllMixerGroup)
						{
							AudioMixerGroup[] tracks = _targetMixer.FindMatchingGroups(GenericTrackName);
							foreach(var track in tracks)
							{
								BroAudioReflection.ExposeParameter(_exposedParaType, track);
							}
						}
						else if (_targetExposeMixerGroup != null)
						{
							BroAudioReflection.ExposeParameter(_exposedParaType, _targetExposeMixerGroup);
						}
					}
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndHorizontal();
			
		}


		private bool DisplayDialog(string functionName)
		{
			return EditorUtility.DisplayDialog(DialogTitle, string.Format(DialogMessage, functionName), Confirm, Cancel);
		}
	}
}