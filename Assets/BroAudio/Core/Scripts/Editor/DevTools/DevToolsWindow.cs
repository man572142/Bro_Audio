#if BroAudio_DevOnly
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using System.Linq;
using Ami.BroAudio;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using System;
using Ami.BroAudio.Editor;

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
        private int _sineWavefrequency = 1000;
        private float _sineWaveDruation = 3f;

		[MenuItem(MenuItem_BroAudio + "Dev Only Tools", priority = DevToolsMenuIndex)]
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

            using (new EditorGUI.DisabledScope(_targetMixer == null))
            {
                GUILayout.Space(30);
                DrawDuplicateLastTrackButton();
                EditorGUILayout.Space();
                DrawAddExposedParameterButton();
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Sine Wave Test Tone", EditorStyles.boldLabel);
            _sineWavefrequency = EditorGUILayout.IntField("Frequency",_sineWavefrequency);
            _sineWaveDruation = EditorGUILayout.FloatField("Duration",_sineWaveDruation);
            if (GUILayout.Button("Generate"))
            {
                GenerateTestTone(_sineWavefrequency, _sineWaveDruation);
            }
		}

		private void DrawDuplicateLastTrackButton()
		{
			if (GUILayout.Button(Function_DuplicateLastTrack, _buttonHeight) && DisplayDialog(Function_DuplicateLastTrack))
			{
				AudioMixerGroup mainTrack = _targetMixer.FindMatchingGroups(MainTrackName)?.Where(x => x.name.Equals(MainTrackName)).FirstOrDefault();
				AudioMixerGroup[] tracks = _targetMixer.FindMatchingGroups(GenericTrackName);
				int tracksCount = tracks.Length;
				if (mainTrack == default || tracks == default)
				{
					Debug.LogError(Utility.LogTitle + $"Can't get the Main track or other BroAudio track");
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

        private void GenerateTestTone(float freq, float duration)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            float[] samples = new float[(int)(sampleRate * duration)];

            for (int i = 0; i < samples.Length; i++)
            {
                double time = (double)i / sampleRate;
                samples[i] = (float)Math.Sin(2 * Math.PI * time * freq);
            }

            string name = $"TestTone_{freq}Hz_0dB";
            string path = EditorUtility.SaveFilePanelInProject("Generate Test Tone", name, "wav", "");
            var audioClip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            audioClip.SetData(samples, 0);

            SavWav.Save(path, audioClip);
        }
    }
}
#endif