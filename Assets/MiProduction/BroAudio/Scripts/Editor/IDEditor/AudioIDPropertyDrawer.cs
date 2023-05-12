using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using UnityEditor.IMGUI.Controls;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.IDEditor
{
	[CustomPropertyDrawer(typeof(AudioID))]
	public class AudioIDPropertyDrawer : PropertyDrawer
	{
		public const string DefaultIDName = "None";
		public const string IDMissing = "Missing";

		private readonly string _missingMessage = IDMissing.ToBold().ToItalics().SetColor(new Color(1f, 0.3f, 0.3f));
		private bool _isInit = false;
		private string _idName = null;

		private GUIStyle _dropdownStyle = EditorStyles.popup;

		private void Init(SerializedProperty idProp,IAudioAsset audioAsset)
		{
			_isInit = true;
			_dropdownStyle.richText = true;
			_dropdownStyle.alignment = TextAnchor.MiddleCenter;

			if (idProp.intValue == 0)
			{
				_idName = DefaultIDName;
				return;
			}
			else if (idProp.intValue < 0)
			{
				_idName = _missingMessage;
				return;
			}

			if (audioAsset != null)
			{
				foreach (var entity in audioAsset.GetAllAudioEntities())
				{
					if (entity.ID == idProp.intValue)
					{
						_idName = entity.Name;
						return;
					}
				}
			}

			idProp.intValue = -1;
			_idName = _missingMessage;	
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty idProp = property.FindPropertyRelative(nameof(AudioID.ID));
			SerializedProperty assetProp = property.FindPropertyRelative(nameof(AudioID.SourceAsset));
			IAudioAsset audioAsset = assetProp.objectReferenceValue as IAudioAsset;

			if (!_isInit)
			{
				Init(idProp, audioAsset);
			}

			EditorScriptingExtension.SplitRectHorizontal(position, 0.4f, 0f, out Rect labelRect, out Rect idRect);
			EditorGUI.LabelField(labelRect, new GUIContent(property.displayName));

			if (EditorGUI.DropdownButton(idRect, new GUIContent(_idName), FocusType.Keyboard, _dropdownStyle))
			{
				var dropdown = new AudioIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
				dropdown.Show(idRect);
			}
			
			if (audioAsset != null && idProp.intValue > 0)
			{
				Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(idRect, 0.7f);
				EditorGUI.DrawRect(audioTypeRect, Utility.GetAudioTypeColor(audioAsset.AudioType));
				EditorGUI.LabelField(audioTypeRect, audioAsset.AudioType.ToString(), GUIStyleHelper.Instance.MiddleCenterText);
			}

			void OnSelect(int id, string name, ScriptableObject asset)
			{
				idProp.intValue = id;
				_idName = name;
				assetProp.objectReferenceValue = asset;
				property.serializedObject.ApplyModifiedProperties();
			}
		}
	} 
}
