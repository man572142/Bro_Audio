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
        private static Vector2 volumeLabelSize => new Vector2(55f,25f);

        public static bool IsDraggingVolumeSlider { get; private set; }
        public static AnimationCurve SpatialBlend => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve ReverbZoneMix => AnimationCurve.Constant(0f, 0f, 1f);
        public static AnimationCurve Spread => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve LogarithmicRolloff => GetLogarithmicCurve(AttenuationMinDistance / AttenuationMaxDistance, 1f, 1f);

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

        private static float SliderFullScale => FullVolume / ((FullDecibelVolume - MinDecibelVolume) / DecibelVoulumeFullScale);

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

        public static bool Contains(this VolumeSliderOptions flags, VolumeSliderOptions targetFlag)
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
            float padding = 3f;
            Rect sliderRect = new Rect(suffixRect) { width = suffixRect.width - EditorGUIUtility.fieldWidth - padding };
            Rect fieldRect = new Rect(suffixRect) { x = sliderRect.xMax + padding, width = EditorGUIUtility.fieldWidth };

#if !UNITY_WEBGL
            if (EditorSetting.ShowVUColorOnVolumeSlider)
            {
                DrawVUMeter(sliderRect, BroAudioGUISetting.VUMaskColor);
            }
            DrawFullVolumeSnapPoint(sliderRect, SliderFullScale, onSwitchSnapMode);

            if (isSnap && CanSnap(currentValue))
            {
                currentValue = FullVolume;
            }

            float newNormalizedValue = DrawVolumeSlider(sliderRect, currentValue, out bool hasSliderChanged, out float newSliderValue);
            float newFloatFieldValue = EditorGUI.FloatField(fieldRect, hasSliderChanged ? newNormalizedValue : currentValue);
            newFloatFieldValue = (float)Math.Floor(newFloatFieldValue * VolumeFieldDigitsMultiplier) / VolumeFieldDigitsMultiplier;
            currentValue = Mathf.Clamp(newFloatFieldValue, 0f, MaxVolume);
#else
				currentValue = GUI.HorizontalSlider(sliderRect, currentValue, 0f, FullVolume);
				currentValue = Mathf.Clamp(EditorGUI.FloatField(fieldRect, currentValue),0f,FullVolume);
#endif
            DrawDecibelValuePeeking(currentValue, padding, sliderRect, newSliderValue);
            return currentValue;

#if !UNITY_WEBGL
            void DrawFullVolumeSnapPoint(Rect sliderPosition, float sliderFullScale, Action onSwitchSnapMode)
            {
                if (onSwitchSnapMode == null)
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

            bool CanSnap(float value)
            {
                float difference = value - FullVolume;
                bool isInLowVolumeSnappingRange = difference < 0f && difference * -1f <= LowVolumeSnappingThreshold;
                bool isInHighVolumeSnappingRange = difference > 0f && difference <= HighVolumeSnappingThreshold;
                return isInLowVolumeSnappingRange || isInHighVolumeSnappingRange;
            }
#endif
        }

        public static void DrawDecibelValuePeeking(float currentValue, float padding, Rect sliderRect, float sliderValue)
        {
            if (Event.current.type == EventType.Repaint)
            {
                float sliderHandlerPos = sliderValue / SliderFullScale * sliderRect.width - (volumeLabelSize.x * 0.5f);
                if(sliderRect.Contains(Event.current.mousePosition))
                {
                    Rect valueTooltipRect = new Rect(sliderRect.x + sliderHandlerPos, sliderRect.y - volumeLabelSize.y - padding, volumeLabelSize.x, volumeLabelSize.y);
                    GUI.skin.window.Draw(valueTooltipRect, false, false, false, false);
                    float dBvalue = currentValue.ToDecibel();
                    string plusSymbol = dBvalue > 0 ? "+" : string.Empty;
                    string volText = plusSymbol + dBvalue.ToString(DbValueStringFormat) + "dB";
                    // ** Don't use EditorGUI.Label(), it will change the keyboard focus, might be a Unity's bug **
                    GUI.Label(valueTooltipRect, volText, GUIStyleHelper.MiddleCenterText);
                }
            }
        }

        public static float DrawVolumeSlider(Rect position, float currentValue,out bool hasChanged, out float newSliderValue)
        {
            float sliderValue = ConvertToSliderValue(currentValue);
            EditorGUI.BeginChangeCheck();
            newSliderValue = GUI.HorizontalSlider(position, sliderValue, 0f, SliderFullScale);
            hasChanged = EditorGUI.EndChangeCheck();
            return ConvertToNomalizedVolume(newSliderValue);

            float ConvertToSliderValue(float vol)
            {
                if (vol > FullVolume)
                {
                    float db = vol.ToDecibel();
                    return (db - MinDecibelVolume) / DecibelVoulumeFullScale * SliderFullScale;
                }
                return vol;
            }

            float ConvertToNomalizedVolume(float sliderValue)
            {
                if (sliderValue > FullVolume)
                {
                    float db = MinDecibelVolume + (sliderValue / SliderFullScale) * DecibelVoulumeFullScale;
                    return db.ToNormalizeVolume(true);
                }
                return sliderValue;
            }
        }

        private static AnimationCurve GetLogarithmicCurve(float timeStart, float timeEnd, float logBase)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioSourceInspector = unityEditorAssembly?.GetType($"UnityEditor.AudioSourceInspector");
            MethodInfo method = audioSourceInspector?.GetMethod("Logarithmic", BindingFlags.NonPublic | BindingFlags.Static);

            return method?.Invoke(null, new object[] { timeStart, timeEnd, logBase }) as AnimationCurve;
        }
    }
}