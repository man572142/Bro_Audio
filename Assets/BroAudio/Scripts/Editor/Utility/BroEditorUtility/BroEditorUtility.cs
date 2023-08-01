using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        private static GlobalSetting _setting = null;
        public static GlobalSetting GlobalSetting
        {
            get
            {
                if(!_setting)
                {
                    _setting = Resources.Load<GlobalSetting>(GlobalSetting.FileName);
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
    }

}