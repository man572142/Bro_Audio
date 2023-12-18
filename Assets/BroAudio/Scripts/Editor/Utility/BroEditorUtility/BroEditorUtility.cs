using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using Ami.BroAudio.Tools;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Ami.Extension;
using static Ami.Extension.StringExtension;
using static Ami.Extension.AudioConstant;
using System.Reflection;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public const string AudioSettingPath = "Project/Audio";
        public const string ProjectAudioSettingFileName = "AudioManager.asset";
        public const string ProjectSettingsFolderName = "ProjectSettings";
        public const string VoiceCountPropertyName = "m_RealVoiceCount";

        private const float LowVolumeSnappingThreshold = 0.05f;
        private const float HighVolumeSnappingThreshold = 0.2f;
        private const string DbValueStringFormat = "0.##";
        private const int VolumeFieldDigitsMultiplier = 1000; // 0.###

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

        public static bool IsTempReservedName(string name)
		{
            string tempName = BroName.TempAssetName;
            if(name.StartsWith(tempName, StringComparison.Ordinal))
            {
                if(name.Length == tempName.Length ||
                    name.Length > tempName.Length && Char.IsNumber(name[tempName.Length]))
				{
                    return true;
				}
            }
            return false;
        }

        public static bool IsInvalidName(string name, out ValidationErrorCode errorCode)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                errorCode = ValidationErrorCode.IsNullOrEmpty;
                return true;
            }

            if (Char.IsNumber(name[0]))
            {
                errorCode = ValidationErrorCode.StartWithNumber;
                return true;
            }

            foreach (char word in name)
            {
                if (!IsValidWord(word))
                {
                    errorCode = ValidationErrorCode.ContainsInvalidWord;
                    return true;
                }

                if (Char.IsWhiteSpace(word))
                {
                    errorCode = ValidationErrorCode.ContainsWhiteSpace;
                    return true;
                }
            }
            errorCode = ValidationErrorCode.NoError;
            return false;
        }

        public static bool IsValidWord(this Char word)
        {
            return IsEnglishLetter(word) || Char.IsNumber(word) || word == '_' || Char.IsWhiteSpace(word);
        }

        public static bool TryGetEntityName(IAudioAsset asset, int id, out string name)
        {
            name = null;
            foreach (var entity in asset.GetAllAudioEntities())
            {
                if (entity.ID == id)
                {
                    name = entity.Name;
                    return true;
                }
            }
            return false;
        }

        public static bool Contains(this DrawedProperty flags, DrawedProperty targetFlag)
		{
            return ((int)flags & (int)targetFlag) != 0;
        }

        public static void DrawVUMeter(Rect vuRect, Color maskColor)
        {
            vuRect.height *= 0.25f;
            vuRect.y += vuRect.height * 0.5f;
            EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent(IconConstant.HorizontalVUMeter).image);
            EditorGUI.DrawRect(vuRect, maskColor);
        }

        public static float DrawVolumeSlider(Rect position, GUIContent label, float currentValue)
		{
            return DrawVolumeSlider(position, label, currentValue, false, null);
        }

        public static float DrawVolumeSlider(Rect position, GUIContent label, float currentValue, bool isSnap, Action onSwitchSnapMode)
        {
            Rect suffixRect = EditorGUI.PrefixLabel(position, label);
            
            float fieldWidth = EditorGUIUtility.fieldWidth;
            float gap = 3f;
            Rect sliderRect = new Rect(suffixRect) { width = suffixRect.width - fieldWidth - gap};
            Rect fieldRect = new Rect(suffixRect) { x = sliderRect.xMax + gap, width = fieldWidth };

#if !UNITY_WEBGL
            if (EditorSetting.ShowVUColorOnVolumeSlider)
            {
                DrawVUMeter(sliderRect, BroAudioGUISetting.VUMaskColor);
            }

            if (isSnap && CanSnap(currentValue))
            {
                currentValue = FullVolume;
            }

            float sliderFullScale = FullVolume / ((FullDecibelVolume - MinDecibelVolume) / DecibelVoulumeFullScale);
            DrawFullVolumeSnapPoint(sliderRect, sliderFullScale, onSwitchSnapMode);

            float sliderValue = ConvertToSliderValue(currentValue, sliderFullScale);
            float newSliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, 0f, sliderFullScale);
            bool hasSliderChanged = sliderValue != newSliderValue;

            float newFloatFieldValue = EditorGUI.FloatField(fieldRect, hasSliderChanged ? ConvertToNomalizedVolume(newSliderValue, sliderFullScale) : currentValue);
            newFloatFieldValue = (float)Math.Floor(newFloatFieldValue * VolumeFieldDigitsMultiplier) / VolumeFieldDigitsMultiplier;
            currentValue = Mathf.Clamp(newFloatFieldValue, 0f, MaxVolume);
#else
				currentValue = GUI.HorizontalSlider(sliderRect, currentValue, 0f, FullVolume);
				currentValue = Mathf.Clamp(EditorGUI.FloatField(fieldRect, currentValue),0f,FullVolume);
#endif
            DrawDecibelValueLabel(suffixRect, currentValue);
            return currentValue;

            void DrawDecibelValueLabel(Rect dbRect, float value)
            {
                // draw an invisible label field for showing tooltip only
                value = Mathf.Log10(value) * DefaultDecibelVolumeScale;
                string plusSymbol = value > 0 ? "+" : string.Empty;
                string volText = plusSymbol + value.ToString(DbValueStringFormat) + "dB";
                EditorGUI.LabelField(suffixRect, new GUIContent() { text = string.Empty, tooltip = volText });
            }

#if !UNITY_WEBGL
            void DrawFullVolumeSnapPoint(Rect sliderPosition, float sliderFullScale, Action onSwitchSnapMode)
            {
                if(onSwitchSnapMode == null)
				{
                    return;
				}

                Rect rect = new Rect(sliderPosition);
                rect.width = 30f;
                rect.x = sliderPosition.x + sliderPosition.width * (FullVolume / sliderFullScale) - (rect.width * 0.5f) + 1f; // add 1 pixel for more precise position
                rect.y -= sliderPosition.height;
                var icon = EditorGUIUtility.IconContent(IconConstant.VolumeSnapPointer);
                EditorGUI.BeginDisabledGroup(!isSnap);
                {
                    GUI.Label(rect, icon);
                }
                EditorGUI.EndDisabledGroup();
                if (GUI.Button(rect, "", EditorStyles.label))
                {
                    onSwitchSnapMode?.Invoke();
                }
            }

            float ConvertToSliderValue(float vol, float sliderFullScale)
            {
                if (vol > FullVolume)
                {
                    float db = vol.ToDecibel(true);
                    return (db - MinDecibelVolume) / DecibelVoulumeFullScale * sliderFullScale;
                }
                return vol;

            }

            float ConvertToNomalizedVolume(float sliderValue, float sliderFullScale)
            {
                if (sliderValue > FullVolume)
                {
                    float db = MinDecibelVolume + (sliderValue / sliderFullScale) * DecibelVoulumeFullScale;
                    return db.ToNormalizeVolume(true);
                }
                return sliderValue;
            }

            bool CanSnap(float value)
            {
                float difference = value - FullVolume;
                bool isInLowVolumeSnappingRange = difference < 0f && difference * -1f <= LowVolumeSnappingThreshold;
                bool isInHighVolumeSnappingRange = difference > 0f && difference <= HighVolumeSnappingThreshold;
                return isInLowVolumeSnappingRange || isInHighVolumeSnappingRange;
            }
#endif
        }

        public static AnimationCurve SpatialBlend => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve ReverbZoneMix => AnimationCurve.Constant(0f, 0f, 1f);
        public static AnimationCurve Spread => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve LogarithmicRolloff => GetLogarithmicCurve(AttenuationMinDistance / AttenuationMaxDistance, 1f, 1f);

        private static AnimationCurve GetLogarithmicCurve(float timeStart, float timeEnd, float logBase)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioSourceInspector = unityEditorAssembly?.GetType($"UnityEditor.AudioSourceInspector");
            MethodInfo method = audioSourceInspector?.GetMethod("Logarithmic", BindingFlags.NonPublic | BindingFlags.Static);

            return method?.Invoke(null, new object[] { timeStart, timeEnd, logBase }) as AnimationCurve;
        }
    }
}