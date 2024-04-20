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
		public Vector2 WindowSize => new Vector2(400f, 550f);

		private MethodInfo _draw3DGUIMethod = null;
		private UnityEditor.Editor _audioSourceEditor = null;
		private readonly Keyframe[] _dummyFrameArray = new Keyframe[] { new Keyframe(0f, 0f) };
		private GameObject _tempObj = null;
		private SerializedProperty _spatialProp = null;

		private MultiLabel _stereoPanLabels => new MultiLabel() { Main = "Stereo Pan", Left = "Left", Right = "Right" };
		private MultiLabel _spatialBlendLabels => new MultiLabel() { Main = "Spatial Blend", Left = "2D", Right = "3D" };

		public static void ShowWindow(SerializedProperty spatialProp)
		{
			var window = GetWindow<SpatialSettingsEditorWindow>();
			window.minSize = window.WindowSize;
			window.maxSize = window.WindowSize;
            window.titleContent = new GUIContent("Spatial Settings");
            window.Init(spatialProp);
#if UNITY_2021_1_OR_NEWER
			EditorApplication.update += Show;
#else
			window.ShowModalUtility();
#endif
			void Show()
			{
                // When the modal window popup, the other GUI should be freezed. But this behavior ridiculously changed after Untiy2021. 
                EditorApplication.update -= Show;
                window.ShowModalUtility();
			}
        }

        private void Init(SerializedProperty spatialProp)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            Type audioSourceInspector = unityEditorAssembly?.GetType($"UnityEditor.AudioSourceInspector");
            _draw3DGUIMethod = audioSourceInspector?.GetMethod("Audio3DGUI", BindingFlags.NonPublic | BindingFlags.Instance);

			_tempObj = new GameObject("AudioSourceInspector");
			_tempObj.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
			AudioSource audioSource = _tempObj.AddComponent<AudioSource>();
			_audioSourceEditor = UnityEditor.Editor.CreateEditor(audioSource);

			if(spatialProp.objectReferenceValue != null && spatialProp.objectReferenceValue is SpatialSetting spatialSetting)
			{
				_spatialProp = spatialProp;
				foreach (SpatialPropertyType propType in Enum.GetValues(typeof(SpatialPropertyType)))
				{
					SetAudioSourceSpatialProperty(propType, spatialSetting);
				}
			}
        }

		private void SetAudioSourceSpatialProperty(SpatialPropertyType propType, SpatialSetting spatialSetting)
        {
			var audioSource = _audioSourceEditor.serializedObject;
			switch (propType)
			{
				case SpatialPropertyType.StereoPan:
					audioSource.FindProperty(AudioSourcePropertyPath.StereoPan).floatValue = spatialSetting.StereoPan;
					break;
				case SpatialPropertyType.DopplerLevel:
					audioSource.FindProperty(AudioSourcePropertyPath.DopplerLevel).floatValue = spatialSetting.DopplerLevel;
					break;
				case SpatialPropertyType.MinDistance:
					audioSource.FindProperty(AudioSourcePropertyPath.MinDistance).floatValue = spatialSetting.MinDistance;
					break;
				case SpatialPropertyType.MaxDistance:
					audioSource.FindProperty(AudioSourcePropertyPath.MaxDistance).floatValue = spatialSetting.MaxDistance;
					break;
				case SpatialPropertyType.SpatialBlend:
					if(!spatialSetting.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D))
					{
						audioSource.FindProperty(AudioSourcePropertyPath.SpatialBlend).animationCurveValue = spatialSetting.SpatialBlend;
					}
					break;
				case SpatialPropertyType.ReverbZoneMix:
					if (!spatialSetting.ReverbZoneMix.IsDefaultCurve(AudioConstant.DefaultReverZoneMix))
					{
						audioSource.FindProperty(AudioSourcePropertyPath.ReverbZoneMix).animationCurveValue = spatialSetting.ReverbZoneMix;
					}
					break;
				case SpatialPropertyType.Spread:
					if (!spatialSetting.Spread.IsDefaultCurve(AudioConstant.DefaultSpread))
					{
						audioSource.FindProperty(AudioSourcePropertyPath.Spread).animationCurveValue = spatialSetting.Spread;
					}
					break;
				case SpatialPropertyType.CustomRolloff:
					if(spatialSetting.RolloffMode != AudioConstant.DefaultRolloffMode)
					{
						audioSource.FindProperty(AudioSourcePropertyPath.CustomRolloff).animationCurveValue = spatialSetting.CustomRolloff;
					}
					break;
				case SpatialPropertyType.RolloffMode:
					audioSource.FindProperty(AudioSourcePropertyPath.RolloffMode).enumValueIndex = (int)spatialSetting.RolloffMode;
					break;
			}
        }

		private void OnDisable()
		{
			if(_audioSourceEditor != null)
			{
				if(_spatialProp != null 
					&& _audioSourceEditor.target is AudioSource audioSource)
				{
					var serializedSpatial = new SerializedObject(_spatialProp.objectReferenceValue);
					serializedSpatial.FindProperty(nameof(SpatialSetting.StereoPan)).floatValue = audioSource.panStereo;
					serializedSpatial.FindProperty(nameof(SpatialSetting.DopplerLevel)).floatValue = audioSource.dopplerLevel;
					serializedSpatial.FindProperty(nameof(SpatialSetting.MinDistance)).floatValue = audioSource.minDistance;
					serializedSpatial.FindProperty(nameof(SpatialSetting.MaxDistance)).floatValue = audioSource.maxDistance;
					serializedSpatial.FindProperty(nameof(SpatialSetting.SpatialBlend)).animationCurveValue = audioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
					serializedSpatial.FindProperty(nameof(SpatialSetting.ReverbZoneMix)).animationCurveValue = audioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
					serializedSpatial.FindProperty(nameof(SpatialSetting.Spread)).animationCurveValue = audioSource.GetCustomCurve(AudioSourceCurveType.Spread);
					if (audioSource.rolloffMode != AudioConstant.DefaultRolloffMode)
					{
						serializedSpatial.FindProperty(nameof(SpatialSetting.CustomRolloff)).animationCurveValue = audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
					}
					serializedSpatial.FindProperty(nameof(SpatialSetting.RolloffMode)).enumValueIndex = (int)audioSource.rolloffMode;
					serializedSpatial.ApplyModifiedProperties();
					_spatialProp.serializedObject.ApplyModifiedProperties();
				}
				
                DestroyImmediate(_tempObj, true);
                DestroyImmediate(_audioSourceEditor);
				_audioSourceEditor = null;
			}
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
			SerializeAnimateCurveValue(spatialBlendProp, _spatialBlendLabels.Main, currValue => Draw2SidesLabelSliderLayout(_spatialBlendLabels, currValue, 0f, 1f));
			EditorGUILayout.Space();

			SerializedProperty reverbZoneProp = _audioSourceEditor.serializedObject.FindProperty(AudioSourcePropertyPath.ReverbZoneMix);
			SerializeAnimateCurveValue(reverbZoneProp, ReverbZoneMixLabel, currValue => EditorGUILayout.Slider(ReverbZoneMixLabel, currValue, 0f, 1.1f)); // reverb zone can accept value up to 1.1
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("3D Sound Settings".ToBold(), GUIStyleHelper.RichText);
            _draw3DGUIMethod?.Invoke(_audioSourceEditor, null);
            _audioSourceEditor.serializedObject.ApplyModifiedProperties();
		}

		private void DrawStereoPan()
		{
			SerializedProperty stereoPanProp = _audioSourceEditor.serializedObject.FindProperty(AudioSourcePropertyPath.StereoPan);
			stereoPanProp.floatValue = Draw2SidesLabelSliderLayout(_stereoPanLabels, stereoPanProp.floatValue, -1f, 1f);
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