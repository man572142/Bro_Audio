using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;

public class DevToolsWindow : EditorWindow
{
	private const string DialogTitle = "Confirm";
	private const string DialogMessage = "This is DevOnly function [{0}],Are you sure you want to execute?";
	private const string Confirm = "Yes";
	private const string Cancel = "No";

	private const string Function_ExposeSendLevel = "Expose All Send Effect Mix Level";
	private const string Function_EnableSendWetMix = "Enable All Send Wet Mix";
	private const string Function_SetSendLevel = "Set All Send Wet Mix Level";

	private AudioMixer _targetMixer = null;

	[MenuItem("BroAudio/Dev Only")]
	public static void ShowWindow()
	{
		EditorWindow window = GetWindow(typeof(DevToolsWindow));
		window.minSize = new Vector2(640f, 360f);
		window.titleContent = new GUIContent("Refelection Tool");
		window.Show();
	}

	private void OnGUI()
	{
		

		EditorGUILayout.Space();
		_targetMixer = EditorGUILayout.ObjectField("AudioMixer",_targetMixer,typeof(AudioMixer),false) as AudioMixer;

		if(!_targetMixer)
		{
			return;
		}
		for(int i = 0; i < 3; i++)
		{
			EditorGUILayout.Space();
		}

		var buttonHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f);

		if (GUILayout.Button(Function_ExposeSendLevel, buttonHeight) && DisplayDialog(Function_ExposeSendLevel))
		{
			EffectParameterReflection.ExposeSendParameter();
		}

		EditorGUILayout.Space();

		if (GUILayout.Button(Function_EnableSendWetMix, buttonHeight) && DisplayDialog(Function_EnableSendWetMix))
		{
			EffectParameterReflection.EnableSendWetMix();
		}

		EditorGUILayout.Space();

		if (GUILayout.Button(Function_SetSendLevel, buttonHeight) && DisplayDialog(Function_SetSendLevel))
		{
			EffectParameterReflection.SetSendWetMixLevel();
		}
	}

	private bool DisplayDialog(string functionName)
	{
		return EditorUtility.DisplayDialog(DialogTitle, string.Format(DialogMessage, functionName), Confirm, Cancel);
	}

}
