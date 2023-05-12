using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using UnityEditor.IMGUI.Controls;
using MiProduction.BroAudio;
using MiProduction.BroAudio.Data;

[CustomPropertyDrawer(typeof(AudioID))]
public class AudioIDPropertyDrawer : PropertyDrawer
{
	public const string DefaultIDName = "None";
	public const string IDMissing = "Missing";

	private bool _isInit = false;
	private string _idName = null;

	private GUIStyle _dropdownStyle = EditorStyles.popup;
	private SerializedProperty _idProp = null;
	private SerializedProperty _assetProp = null;

	private void Init(SerializedProperty property)
	{
		_dropdownStyle.richText = true;
		_dropdownStyle.alignment = TextAnchor.MiddleCenter;

		_idProp = property.FindPropertyRelative(nameof(AudioID.ID));
		_assetProp = property.FindPropertyRelative(nameof(AudioID.SourceAsset));

		if (_idProp.intValue == 0)
		{
			_idName = DefaultIDName;
			return;
		}
		else if (_idProp.intValue < 0)
		{
			_idName = GetMissingMessage();
			return;
		}

		IAudioAsset audioAsset = _assetProp.objectReferenceValue as IAudioAsset;
		if (audioAsset != null)
		{
			foreach (var entity in audioAsset.GetAllAudioEntities())
			{
				if (entity.ID == _idProp.intValue)
				{
					_idName = entity.Name;
					return;
				}
			}
		}

		// TODO: 有用到ID的地方都要判定是否為負的，若為負的則Log說ID MISSING
		_idProp.intValue = -1;
		_idName = GetMissingMessage();
		_isInit = true;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (!_isInit)
		{
			Init(property);
		}

		EditorScriptingExtension.SplitRectHorizontal(position, 0.4f, 0f, out Rect labelRect, out Rect idRect);
		EditorGUI.LabelField(labelRect, new GUIContent(property.displayName));

		if (EditorGUI.DropdownButton(idRect, new GUIContent(_idName), FocusType.Keyboard, _dropdownStyle))
		{
			var dropdown = new AudioIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
			dropdown.Show(idRect);
		}

		void OnSelect(int id, string name,ScriptableObject asset)
		{
			_idProp.intValue = id;
			_idName = name;
			_assetProp.objectReferenceValue = asset;
			property.serializedObject.ApplyModifiedProperties();
		}
	}

	private string GetMissingMessage() => IDMissing.ToBold().ToItalics().SetColor(new Color(1f,0.3f,0.3f));
}
