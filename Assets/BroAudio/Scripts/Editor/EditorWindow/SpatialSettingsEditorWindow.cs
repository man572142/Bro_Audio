using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;

public class SpatialSettingsEditorWindow : EditorWindow
{
	private static class PropertyPath
	{
		public const string OutputAudioMixerGroup = "OutputAudioMixerGroup";
		public const string AudioClip = "m_audioClip";
		public const string PlayOnAwake = "m_PlayOnAwake";
		public const string Volume = "m_Volume";
		public const string Pitch = "m_Pitch";
		public const string Loop = "Loop";
		public const string Mute = "Mute";
		public const string Spatialize = "Spatialize";
		public const string SpatializePostEffects = "SpatializePostEffects";
		public const string Priority = "Priority";
		public const string DopplerLevel = "DopplerLevel";
		public const string MinDistance = "MinDistance";
		public const string MaxDistance = "MaxDistance";
		public const string StereoPan = "Pan2D";
		public const string VolumeRolloff = "rolloffMode";
		public const string BypassEffects = "BypassEffects";
		public const string BypassListenerEffects = "BypassListenerEffects";
		public const string BypassReverbZones = "BypassReverbZones";
		public const string RolloffCustomCurve = "rolloffCustomCurve";
		public const string SpatialBlend = "panLevelCustomCurve";
		public const string Spread = "spreadCustomCurve";
		public const string ReverbZoneMix = "reverbZoneMixCustomCurve";
	}

	public const string ReverbZoneMixLabel = "Reverb Zone Mix";

	private GameObject _prefab = null;
	private Type _audioSourceInspector = null;
	private MethodInfo _draw3DGUIMethod = null;
	private Editor _audioSourceEditor = null;
	private readonly Keyframe[] _dummyFrameArray = new Keyframe[] { new Keyframe(0f, 0f) };

	private MultiLabel _stereoPanLabels => new MultiLabel() { Main = "Stereo Pan", Left = "Left", Right = "Right"};
	private MultiLabel _spatialBlendLabels => new MultiLabel() { Main = "Spatial Blend", Left = "2D", Right = "3D" };

#if BroAudio_DevOnly
	[MenuItem("BroAudio/Spatial Setting Window")]
#endif
	public static void ShowWindow()
	{
		EditorWindow window = GetWindow<SpatialSettingsEditorWindow>();
		Vector2 size = new Vector2(400f, 550f);
		window.minSize = size;
		window.maxSize = size;
		window.titleContent = new GUIContent("Spatial Setting");
		window.ShowModal();
	}

	private void OnEnable()
	{
		Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
		_audioSourceInspector = unityEditorAssembly?.GetType($"UnityEditor.AudioSourceInspector");

		_draw3DGUIMethod = _audioSourceInspector?.GetMethod("Audio3DGUI", BindingFlags.NonPublic | BindingFlags.Instance);

		_prefab = Resources.Load<GameObject>("Editor/AudioSourceInspector");
		AudioSource audioSource = _prefab.AddComponent<AudioSource>();
		
		_audioSourceEditor = Editor.CreateEditor(audioSource);
	}

	private void OnDisable()
	{

		ResetComponent();
		DestroyImmediate(_audioSourceEditor);
	}

	private void ResetComponent()
	{
		// Unsupported.SmartReset(_audioSourceEditor.target); 
		// The code above works too, but such a misty code is hard to trust.

		DestroyImmediate(_audioSourceEditor.target,true);
	}

	private void OnGUI()
	{
		if (_audioSourceEditor == null)
		{
			return;
		}

		DrawStereoPan();
		EditorGUILayout.Space();

		SerializedProperty spatialBlendProp = _audioSourceEditor.serializedObject.FindProperty(PropertyPath.SpatialBlend);
		SerializeAnimateCurveValue(spatialBlendProp,_spatialBlendLabels.Main, currValue => Draw2SidesLabelSlider(_spatialBlendLabels, currValue, 0f, 1f));
		EditorGUILayout.Space();

		SerializedProperty reverbZoneProp = _audioSourceEditor.serializedObject.FindProperty(PropertyPath.ReverbZoneMix);
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
		SerializedProperty stereoPanProp = _audioSourceEditor.serializedObject.FindProperty(PropertyPath.StereoPan);
		stereoPanProp.floatValue = Draw2SidesLabelSlider(_stereoPanLabels, stereoPanProp.floatValue, -1f, 1f);
	}

	private void SerializeAnimateCurveValue(SerializedProperty serializedProperty,string label,Func<float,float> onDrawSlider)
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
