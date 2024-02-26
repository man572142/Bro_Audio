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
using static Ami.Extension.EditorScriptingExtension;
using System.Reflection;

namespace Ami.BroAudio.Editor
{
    public static partial class BroEditorUtility
    {
        public enum RandomRangeSliderType
        {
            Default, Logarithmic, BroVolume,
        }

        public const string AudioSettingPath = "Project/Audio";
        public const string ProjectAudioSettingFileName = "AudioManager.asset";
        public const string ProjectSettingsFolderName = "ProjectSettings";
        public const string VoiceCountPropertyName = "m_RealVoiceCount";

        private const float LowVolumeSnappingThreshold = 0.05f;
        private const float HighVolumeSnappingThreshold = 0.2f;
        private const string DbValueStringFormat = "0.##";
        private static Vector2 volumeLabelSize => new Vector2(55f,25f);
        public static readonly float[] VolumeSplitPoints = { -80f, -60f, -36f, -24f, -12f, -6f, 0f, 6f, 20f };

        public static float VolumeScale => 1f / (VolumeSplitPoints.Length - 1);
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
                    _runtimeSetting = Resources.Load<RuntimeSetting>(BroName.RuntimeSettingFileName);
                    if (!_runtimeSetting)
                    {
                        GlobalSettingEditorWindow.ShowWindowWithMessage(GlobalSettingEditorWindow.OpenMessage.RuntimeSettingFileMissing);
                    }
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                }
                return _runtimeSetting;
            }
        }

#if UNITY_EDITOR
        private static EditorSetting _editorSetting = null;
        public static EditorSetting EditorSetting
        {
            get
            {
                if (!_editorSetting)
                {
                    _editorSetting = Resources.Load<EditorSetting>(BroName.EditorSettingPath);
                    if (!_editorSetting)
                    {
                        GlobalSettingEditorWindow.ShowWindowWithMessage(GlobalSettingEditorWindow.OpenMessage.RuntimeSettingFileMissing);
                    }
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                }
                return _editorSetting;
            }
        } 
#endif

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
            return (name.Length == tempName.Length || name.Length > tempName.Length && Char.IsNumber(name[tempName.Length]))
                && name.StartsWith(tempName, StringComparison.Ordinal);
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
            DrawFullVolumeSnapPoint(sliderRect, onSwitchSnapMode);

            float newNormalizedValue = DrawVolumeSlider(sliderRect, currentValue, out bool hasSliderChanged, out float newSliderValue);
            float newFloatFieldValue = EditorGUI.FloatField(fieldRect, hasSliderChanged ? newNormalizedValue : currentValue);
            currentValue = Mathf.Clamp(newFloatFieldValue, 0f, MaxVolume);
            if (isSnap && CanSnap(currentValue))
			{
				currentValue = FullVolume;
			}
			DrawDecibelValuePeeking(currentValue, padding, sliderRect, newSliderValue);
#else
            currentValue = GUI.HorizontalSlider(sliderRect, currentValue, 0f, FullVolume);
			currentValue = Mathf.Clamp(EditorGUI.FloatField(fieldRect, currentValue),0f,FullVolume);
#endif
			
            return currentValue;

#if !UNITY_WEBGL
            void DrawFullVolumeSnapPoint(Rect sliderPosition, Action onSwitch)
            {
                if (onSwitch == null)
                {
                    return;
                }

				float scale = 1f / (VolumeSplitPoints.Length - 1);
				float sliderValue = VolumeToSlider(FullVolume);

				Rect rect = new Rect(sliderPosition);
                rect.width = 30f;
                rect.x = sliderPosition.x + sliderPosition.width * sliderValue - (rect.width * 0.5f) + 1f; // add 1 pixel for more precise position
                rect.y -= sliderPosition.height;
                var icon = EditorGUIUtility.IconContent(IconConstant.VolumeSnapPointer);
                icon.tooltip = "Toggle full volume snapping";
                EditorGUI.BeginDisabledGroup(!isSnap);
                {
                    GUI.Label(rect, icon);
                }
                EditorGUI.EndDisabledGroup();
                if (GUI.Button(rect, "", EditorStyles.label))
                {
                    onSwitch?.Invoke();
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

        public static float DrawVolumeSlider(Rect position, float currentValue,out bool hasChanged, out float newSliderInFullScale)
		{
			float sliderValue = VolumeToSlider(currentValue);

			EditorGUI.BeginChangeCheck();
			sliderValue = GUI.HorizontalSlider(position, sliderValue, 0f, 1f);
			newSliderInFullScale = sliderValue * SliderFullScale;
			hasChanged = EditorGUI.EndChangeCheck();
			if(hasChanged)
            {
                return SliderToVolume(sliderValue);
			}
			return currentValue;
		}

        public static float DrawRandomRangeVolumeSlider(Rect position, GUIContent label, ref float min, ref float max, float minLimit, float maxLimit, float fieldWidth, Action<Rect> onGetSliderRect = null)
        {
            Rect sliderRect = DrawMinMaxLabelAndField(position, label, ref min, ref max, fieldWidth, onGetSliderRect);
            float minSlider = VolumeToSlider(min);
            float maxSlider = VolumeToSlider(max);

            EditorGUI.MinMaxSlider(sliderRect, ref minSlider, ref maxSlider, 0f, 1f);

            min = SliderToVolume(minSlider);
            max = SliderToVolume(maxSlider);
			return max;
		}

		private static float VolumeToSlider(float vol)
		{
			float decibelVol = vol.ToDecibel();
			for (int i = 0; i < VolumeSplitPoints.Length; i++)
			{
				if (i + 1 >= VolumeSplitPoints.Length)
				{
					return 1f;
				}
				else if (decibelVol >= VolumeSplitPoints[i] && decibelVol < VolumeSplitPoints[i + 1])
				{
					float currentStageSliderValue = VolumeScale * i;
					float range = Mathf.Abs(VolumeSplitPoints[i + 1] - VolumeSplitPoints[i]);
					float stageProgress = Mathf.Abs(decibelVol - VolumeSplitPoints[i]) / range;
					return currentStageSliderValue + stageProgress * VolumeScale;
				}
			}
			return 0f;
		}

        private static float SliderToVolume(float sliderValue)
        {
			if (sliderValue == 1f)
			{
				return MaxVolume;
			}
			int newStageIndex = (int)(sliderValue / VolumeScale);
			float progress = (sliderValue % VolumeScale) / VolumeScale;
			float range = Mathf.Abs(VolumeSplitPoints[newStageIndex + 1] - VolumeSplitPoints[newStageIndex]);
			float decibelResult = VolumeSplitPoints[newStageIndex] + range * progress;
			return decibelResult.ToNormalizeVolume();
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