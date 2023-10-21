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

		private GUIStyle _dropdownStyle = new GUIStyle(EditorStyles.popup);

		private void Init(SerializedProperty idProp,SerializedProperty assetProp)
		{
            _isInit = true;
			_dropdownStyle.richText = true;
			_dropdownStyle.alignment = TextAnchor.MiddleCenter;

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

            AudioAsset asset = assetProp.objectReferenceValue as AudioAsset;
            if (asset == null || !BroEditorUtility.TryGetEntityName(asset,idProp.intValue,out _entityName))
            {
				LoadAllAssetToFindEntity();
            }

			void LoadAllAssetToFindEntity()
			{
				// TODO: maybe use something like context menu to execute this
                // todo: might have performance impact if the library is huge. we should only load core data once to find entity for a script's all AudioID, not load it foreach audioID
                BroAudioType audioType = Utility.GetAudioType(idProp.intValue);
                if (!audioType.IsConcrete())
                {
                    SetToMissing(idProp);
                    return;
                }

                List<string> guidList = BroEditorUtility.GetGUIDListFromJson();
                foreach (string guid in guidList)
                {
                    // todo : organized guid by audioType might help improve performance, because we don't need to load that asset if audio type isn't match 
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) as AudioAsset;
                    if (asset != null && asset.AudioType == audioType &&
                        BroEditorUtility.TryGetEntityName(asset, idProp.intValue, out _entityName))
                    {
                        assetProp.objectReferenceValue = asset;
                        assetProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        return;
                    }
                }
                SetToMissing(idProp);
            }

            void SetToMissing(SerializedProperty idProp)
            {
                idProp.intValue = -1;
                _entityName = _missingMessage;
            }
        }

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			SerializedProperty idProp = property.FindPropertyRelative(nameof(AudioID.ID));
			SerializedProperty assetProp = property.FindPropertyRelative(nameof(AudioID.SourceAsset));

			if (!_isInit)
			{
				Init(idProp, assetProp);
			}

			EditorScriptingExtension.SplitRectHorizontal(position, 0.4f, 0f, out Rect labelRect, out Rect idRect);
            EditorGUI.LabelField(labelRect, new GUIContent(property.displayName, ToolTip));

			if (EditorGUI.DropdownButton(idRect, new GUIContent(_entityName, ToolTip), FocusType.Keyboard, _dropdownStyle))
			{
				var dropdown = new AudioIDAdvancedDropdown(new AdvancedDropdownState(), OnSelect);
				dropdown.Show(idRect);
			}

            IAudioAsset audioAsset = assetProp.objectReferenceValue as IAudioAsset;
            if (BroEditorUtility.EditorSetting.ShowAudioTypeOnAudioID && audioAsset != null && idProp.intValue > 0)
			{
                Rect audioTypeRect = EditorScriptingExtension.DissolveHorizontal(idRect, 0.7f);
				EditorGUI.DrawRect(audioTypeRect, BroEditorUtility.EditorSetting.GetAudioTypeColor(audioAsset.AudioType));
				EditorGUI.LabelField(audioTypeRect, audioAsset.AudioType.ToString(), GUIStyleHelper.MiddleCenterText);
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
