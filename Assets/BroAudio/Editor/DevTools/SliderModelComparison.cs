using UnityEngine;
using UnityEditor;
using Ami.Extension;
using System;
using Ami.Extension.Reflection;
using static Ami.BroAudio.Tools.BroName;
using System.Reflection;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

namespace Ami.BroAudio.Editor.DevTool
{
	public class SliderModelComparison : MiEditorWindow
	{
		private const float SliderFieldGap = 5f;

		private float _currentValue = default;
		private MethodInfo _unityMixerSliderAlgorithm = null;
		private object[] _unityMixerSliderAlgorithmParameters = null;
		private object[] _reverseSliderAlgorithmParameter = null;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

#if BroAudio_DevOnly
        [MenuItem(MenuItem_BroAudio + "SliderModelComparison", priority = DevToolsMenuIndex + 2)]
#endif

        public static void ShowWindow()
		{
			var window = GetWindow<SliderModelComparison>();
			window.titleContent = new GUIContent("Slider Model Comparison");
			window.minSize = new Vector2(600f, 350f);
			window.Show();
		}


		private void OnEnable()
		{
			_currentValue = AudioConstant.MinVolume;

			ReflectUnityAudioMixerSlider();
		}

		private void ReflectUnityAudioMixerSlider()
		{
			Type mixerClass = ClassReflectionHelper.GetUnityAudioEditorClass("AudioMixerController");
			_unityMixerSliderAlgorithm = mixerClass.GetMethod("VolumeToScreenMapping", BindingFlags.Static | BindingFlags.Public);
			_unityMixerSliderAlgorithmParameters = new object[] { 0f, 0f, true };

			_reverseSliderAlgorithmParameter = new object[] { 0f, 0f, false };
		}

		protected override void OnGUI()
		{
			base.OnGUI();
			DrawEmptyLine(2);

			DrawSliderInfo("Linear", DrawLinearSlider);
			DrawSliderInfo("Logarithmic", DrawLogarthmicSlider);
			DrawSliderInfo("Unity Audio Mixer", DrawUnityAudioMixer);
			DrawSliderInfo("Bro Audio", DrawBroSlider);

			EditorGUI.LabelField(GetRectAndIterateLine(position).Scoping(position), $"{_currentValue.ToDecibel()} dB".SetSize(20),GUIStyleHelper.MiddleCenterRichText);
		}

		private void DrawSliderInfo(string label, Action<Rect> onDrawSlider)
		{
			GUIContent labelContent = new GUIContent(label.SetSize(15));
			Rect labelRect = GetRectAndIterateLine(position).Scoping(position);
			labelRect.x += 15f;
			Rect suffixRect = EditorGUI.PrefixLabel(labelRect, labelContent, GUIStyleHelper.RichText);
			suffixRect.width *= 0.9f;
			DrawEmptyLine(1);
			onDrawSlider?.Invoke(suffixRect);
			DrawEmptyLine(1);
		}

		private void DrawLinearSlider(Rect rect)
		{
			_currentValue = EditorGUI.Slider(rect, _currentValue, AudioConstant.MinVolume, AudioConstant.MaxVolume);
		}

		private void DrawLogarthmicSlider(Rect rect)
		{
			_currentValue = EditorScriptingExtension.DrawLogarithmicSlider_Horizontal(rect, _currentValue, AudioConstant.MinVolume, AudioConstant.MaxVolume);
		}

		private void DrawUnityAudioMixer(Rect rect)
		{
			rect.width -= EditorGUIUtility.fieldWidth + SliderFieldGap;
			Rect fieldRect = new Rect(rect) { width = EditorGUIUtility.fieldWidth, x = rect.xMax + SliderFieldGap };

			_unityMixerSliderAlgorithmParameters[0] = _currentValue.ToDecibel();
			_unityMixerSliderAlgorithmParameters[1] = rect.width;
			float screenPoint = (float)_unityMixerSliderAlgorithm.Invoke(null, _unityMixerSliderAlgorithmParameters);
			float sliderValue = 1 - (screenPoint / rect.width);
			EditorGUI.BeginChangeCheck();
			sliderValue = GUI.HorizontalSlider(rect, sliderValue, 0f, 1f);
			
			if(EditorGUI.EndChangeCheck())
			{
				_reverseSliderAlgorithmParameter[0] = (1 - sliderValue) * rect.width;
				_reverseSliderAlgorithmParameter[1] = rect.width;
				float newVolInDecibel = (float)_unityMixerSliderAlgorithm.Invoke(null, _reverseSliderAlgorithmParameter);
				_currentValue = newVolInDecibel.ToNormalizeVolume();
			}

			_currentValue = EditorGUI.FloatField(fieldRect, _currentValue);
		}

		private void DrawBroSlider(Rect rect)
		{
			rect.width -= EditorGUIUtility.fieldWidth + SliderFieldGap;
			Rect fieldRect = new Rect(rect) { width = EditorGUIUtility.fieldWidth, x = rect.xMax + SliderFieldGap };
			_currentValue = BroEditorUtility.DrawVolumeSlider(rect, _currentValue, out bool hasChanged, out float newSliderValue);
			_currentValue = EditorGUI.FloatField(fieldRect, _currentValue);
		}
	}
}