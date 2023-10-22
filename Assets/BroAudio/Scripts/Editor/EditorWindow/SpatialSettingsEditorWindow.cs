using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.BroEditorUtility;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public class SpatialSettingsEditorWindow : EditorWindow
	{
		public const string ReverbZoneMixLabel = "Reverb Zone Mix";

		public Action<SpatialSettings> OnCloseWindow;

		private MethodInfo _draw3DGUIMethod = null;
		private UnityEditor.Editor _audioSourceEditor = null;
		private readonly Keyframe[] _dummyFrameArray = new Keyframe[] { new Keyframe(0f, 0f) };

		private MultiLabel _stereoPanLabels => new MultiLabel() { Main = "Stereo Pan", Left = "Left", Right = "Right" };
		private MultiLabel _spatialBlendLabels => new MultiLabel() { Main = "Spatial Blend", Left = "2D", Right = "3D" };

		public static void ShowWindow(SerializedProperty settingsProp, Action<SpatialSettings> onCloseWindow)
		{
			var window = GetWindow<SpatialSettingsEditorWindow>();
			Vector2 size = new Vector2(400f, 550f);
			window.minSize = size;
			window.maxSize = size;
			window.titleContent = new GUIContent("Spatial Settings");
			window.OnCloseWindow = onCloseWindow;
            window.Init(settingsProp);
            window.ShowModal();
		}

        private void Init(SerializedProperty settingsProp)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioSourceInspector = unityEditorAssembly?.GetType($"UnityEditor.AudioSourceInspector");
            _draw3DGUIMethod = audioSourceInspector?.GetMethod("Audio3DGUI", BindingFlags.NonPublic | BindingFlags.Instance);

            GameObject prefab = Resources.Load<GameObject>("Editor/AudioSourceInspector");
            AudioSource audioSource = prefab.AddComponent<AudioSource>();
            _audioSourceEditor = UnityEditor.Editor.CreateEditor(audioSource);

			foreach(SpatialPropertyType propType in Enum.GetValues(typeof(SpatialPropertyType)))
			{
				SetAudioSourceProperty(propType,settingsProp);
			}
        }

		private void SetAudioSourceProperty(SpatialPropertyType propType, SerializedProperty settingsProp)
        {
            SerializedProperty audioSourceProp = GetAudioSourceProperty(_audioSourceEditor.serializedObject, propType);
            SerializedProperty settingRelativeProp = GetSpatialSettingsProperty(settingsProp, propType);

            if (audioSourceProp.propertyType == SerializedPropertyType.Float)
            {
                audioSourceProp.floatValue = settingRelativeProp.floatValue;
            }
            else if (audioSourceProp.propertyType == SerializedPropertyType.AnimationCurve)
            {
                audioSourceProp.SafeSetCurve(settingRelativeProp.animationCurveValue);
            }
        }

        private void OnDisable()
		{
			if(_audioSourceEditor != null)
			{
                OnCloseWindow?.Invoke(GetSpatialSettings());

                // Unsupported.SmartReset(_audioSourceEditor.target); 
                // The code above works too, but such a misty code is hard to trust. remove and add it back to reset it would be better. 
                DestroyImmediate(_audioSourceEditor.target, true);

                DestroyImmediate(_audioSourceEditor);
				_audioSourceEditor = null;
            }
		}

        private SpatialSettings GetSpatialSettings()
        {
			SerializedObject so = _audioSourceEditor.serializedObject;
            SpatialSettings settings = new SpatialSettings()
            {
                StereoPan = GetAudioSourceProperty(so,SpatialPropertyType.StereoPan).floatValue,
                DopplerLevel = GetAudioSourceProperty(so, SpatialPropertyType.DopplerLevel).floatValue,
                MinDistance = GetAudioSourceProperty(so, SpatialPropertyType.MinDistance).floatValue,
                MaxDistance = GetAudioSourceProperty(so, SpatialPropertyType.MaxDistance).floatValue,
                SpatialBlend = GetAudioSourceProperty(so, SpatialPropertyType.SpatialBlend).animationCurveValue,
                ReverbZoneMix = GetAudioSourceProperty(so, SpatialPropertyType.ReverbZoneMix).animationCurveValue,
                Spread = GetAudioSourceProperty(so, SpatialPropertyType.Spread).animationCurveValue,
                CustomRolloff = GetAudioSourceProperty(so, SpatialPropertyType.CustomRolloff).animationCurveValue,
            };
            return settings;
        }

		private void OnGUI()
		{
			if (_audioSourceEditor == null)
			{
				return;
			}

			DrawStereoPan();
			EditorGUILayout.Space();

			SerializedProperty spatialBlendProp = _audioSourceEditor.serializedObject.FindProperty(AudioSourcePropertyPath.SpatialBlend);
			SerializeAnimateCurveValue(spatialBlendProp, _spatialBlendLabels.Main, currValue => Draw2SidesLabelSlider(_spatialBlendLabels, currValue, 0f, 1f));
			EditorGUILayout.Space();

			SerializedProperty reverbZoneProp = _audioSourceEditor.serializedObject.FindProperty(AudioSourcePropertyPath.ReverbZoneMix);
			SerializeAnimateCurveValue(reverbZoneProp, ReverbZoneMixLabel, currValue => EditorGUILayout.Slider(ReverbZoneMixLabel, currValue, 0f, 1.1f)); // reverb zone can accept value up to 1.1
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("3D Sound Settings".ToBold(), GUIStyleHelper.RichText);
			if (_draw3DGUIMethod != null)
			{
				_draw3DGUIMethod.Invoke(_audioSourceEditor, null);
			}
		}

		private void DrawStereoPan()
		{
			SerializedProperty stereoPanProp = _audioSourceEditor.serializedObject.FindProperty(AudioSourcePropertyPath.StereoPan);
			stereoPanProp.floatValue = Draw2SidesLabelSlider(_stereoPanLabels, stereoPanProp.floatValue, -1f, 1f);
		}

		private void SerializeAnimateCurveValue(SerializedProperty serializedProperty, string label, Func<float, float> onDrawSlider)
		{
			if (serializedProperty.animationCurveValue.length > 1)
			{
				GUIStyle leftMiniGrey = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
				leftMiniGrey.alignment = TextAnchor.MiddleLeft;
				EditorGUILayout.LabelField(label, "Controlled by curve", leftMiniGrey);
			}
			else if (serializedProperty.animationCurveValue.length == 1)
			{
				EditorGUI.BeginChangeCheck();
				float newValue = onDrawSlider.Invoke(serializedProperty.animationCurveValue[0].value);
				if (EditorGUI.EndChangeCheck())
				{
					AnimationCurve curve = serializedProperty.animationCurveValue;
					_dummyFrameArray[0] = new Keyframe(0f, newValue);
					curve.keys = _dummyFrameArray;
					serializedProperty.animationCurveValue = curve;
					serializedProperty.serializedObject.ApplyModifiedProperties();
				}
			}
		}
	}

}