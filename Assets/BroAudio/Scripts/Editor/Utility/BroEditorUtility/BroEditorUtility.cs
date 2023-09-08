using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public const string AudioSettingPath = "Project/Audio";
        public const string ProjectAudioSettingFileName = "AudioManager.asset";
        public const string ProjectSettingsFolderName = "ProjectSettings";
        public const string VoiceCountPropertyName = "m_RealVoiceCount";


        private static RuntimeSetting _runtimeSetting = null;
        public static RuntimeSetting RuntimeSetting
        {
            get
            {
                if(!_runtimeSetting)
                {
                    _runtimeSetting = Resources.Load<RuntimeSetting>(RuntimeSetting.FilePath);
                    if (!_runtimeSetting)
                    {
                        GlobalSettingEditorWindow.ShowWindowWithMessage(GlobalSettingEditorWindow.OpenMessage.RuntimeSettingFileMissing);
                    }
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                }
                return _runtimeSetting;
            }
        }

        private static EditorSetting _editorSetting = null;
        public static EditorSetting EditorSetting
        {
            get
            {
                if (!_editorSetting)
                {
                    _editorSetting = Resources.Load<EditorSetting>(Editor.EditorSetting.FilePath);
                    if (!_editorSetting)
                    {
                        GlobalSettingEditorWindow.ShowWindowWithMessage(GlobalSettingEditorWindow.OpenMessage.RuntimeSettingFileMissing);
                    }
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                }
                return _editorSetting;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.EnteredPlayMode)
            {
                _runtimeSetting = null;
            }
        }

        public static int GetProjectSettingRealAudioVoices()
		{
            string path = GetFullFilePath(ProjectSettingsFolderName, ProjectAudioSettingFileName);
            if (!File.Exists(path))
            {
                return default;
            }

            using (StreamReader stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    string lineText = stream.ReadLine();
                    if (!lineText.Contains(VoiceCountPropertyName))
                    {
                        continue;
                    }

                    string value = string.Empty;
                    for (int i = 0; i < lineText.Length; i++)
                    {
                        if (char.IsNumber(lineText[i]))
                        {
                            value += lineText[i];
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return int.Parse(value);
                    }
                }
            }
            return default;
        }
    }

}