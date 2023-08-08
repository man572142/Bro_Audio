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


        private static GlobalSetting _setting = null;
        public static GlobalSetting GlobalSetting
        {
            get
            {
                if(!_setting)
                {
                    _setting = Resources.Load<GlobalSetting>(GlobalSetting.FilePath);
                    if (!_setting)
                    {
                        GlobalSettingEditorWindow.ShowWindowWithMessage(GlobalSettingEditorWindow.OpenMessage.SettingAssetFileMissing);
                    }
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                }
                return _setting;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.EnteredPlayMode)
            {
                _setting = null;
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