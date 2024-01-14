using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using UnityEditor.IMGUI.Controls;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioID))]
	public class AudioIDPropertyDrawer : PropertyDrawer
	{
		public const string DefaultIDName = "None";
		public const string IDMissing = "Missing";
		public const string ToolTip = "refering to an AudioEntity";


        private readonly string _missingMessage = IDMissing.ToBold().ToItalics().SetColor(new Color(1f, 0.3f, 0.3f));
		private bool _isInit = false;
		private string _entityName = null;

		private GUIStyle _dropdownStyle = new GUIStyle(EditorStyles.popup) { richText = true};

		private void Init(SerializedProperty idProp,SerializedProperty assetProp)
		{
            _isInit = true;

			if (idProp.intValue == 0)
			{
				_entityName = DefaultIDName;
				return;
			}
			else if (idProp.intValue < 0)
			{
				_entityName = _missingMessage;
				return;
			}

			BroAudioType audioType = Utility.GetAudioType(idProp.intValue);
			if (!audioType.IsConcrete())
			{
				SetToMissing(idProp);
				return;
			}

			AudioAsset asset = assetProp.objectReferenceValue as AudioAsset;
            if (asset != null && BroEditorUtility.TryGetEntityName(asset,idProp.intValue,out _entityName))
            {
				return;
            }

            // TODO: Initializing this whenever an AudioID is created is not efficient. 
            List<string> guidList = BroEditorUtility.GetGUIDListFromJson();
			foreach (string guid in guidList)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) as AudioAsset;
				if (asset != null && BroEditorUtility.TryGetEntityName(asset, idProp.intValue, out _entityName))
				{
					assetProp.objectReferenceValue = asset;
					assetProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
					return;
				}
			}
			SetToMissing(idProp);

			void SetToMissing(SerializedProperty idProp)
            {
                idProp.intValue = -1;
                _entityName = _missingMessage;
            }
        }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty idProp = property.FindPropertyRelative(nameof(AudioID.ID));
			SerializedProperty assetProp = property.FindPropertyRelative(AudioID.NameOf.SourceAsset);

			if (!_isInit)
			{
				Init(idProp, assetProp);
			}

            Rect suffixRect = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName, ToolTip));

			if (EditorGUI.DropdownButton(suffixRect, new GUIContent(_entityName, ToolTip), FocusType.Keyboard, _dropdownStyle))
			{
				var dropdown = new AudioIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
				dropdown.Show(suffixRect);
			}

            IAudioAsset audioAsset = assetProp.objectReferenceValue as IAudioAsset;
            if (BroEditorUtility.EditorSetting.ShowAudioTypeOnAudioID && audioAsset != null && idProp.intValue > 0)
			{
				BroAudioType audioType = Utility.GetAudioType(idProp.intValue);
                Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(suffixRect, 0.7f);
				EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(audioType));
				EditorGUI.LabelField(audioTypeRect, audioType.ToString(), GUIStyleHelper.MiddleCenterText);
			}

			void OnSelect(int id, string name, ScriptableObject asset)
			{
				idProp.intValue = id;
				_entityName = name;
				assetProp.objectReferenceValue = asset;
				property.serializedObject.ApplyModifiedProperties();
			}
		}
	} 
}
