using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace Ami.BroAudio.Editor
{
	[CustomEditor(typeof(SoundTrigger))]
	public class SoundTriggerEditor : UnityEditor.Editor
	{
        public const string Header_Triggers = "Triggers";
		

		private ReorderableList _reorderableList = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();

		private void OnEnable()
		{
			InitReorderableList();
		}

        private void InitReorderableList()
		{
			SerializedProperty triggersProp = serializedObject.FindProperty(SoundTrigger.NameOf.Triggers);
			_reorderableList = new ReorderableList(serializedObject, triggersProp)
			{
				drawHeaderCallback = OnDrawHeader,
				drawElementCallback = OnDrawElement,
				elementHeightCallback = OnGetElementHeight,
			};

			void OnDrawHeader(Rect rect)
			{
				EditorGUI.LabelField(rect, Header_Triggers);
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				var elementProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(rect, elementProp);
            }

            float OnGetElementHeight(int index)
            {
                var elementProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				return EditorGUI.GetPropertyHeight(elementProp);
            }
        }

        public override void OnInspectorGUI()
		{
			//var parameterProp = serializedObject.FindProperty(SoundTrigger.NameOf.DefaultParameter);
			//EditorGUILayout.LabelField("Default Parameter", EditorStyles.boldLabel);

			//var sourceProp = parameterProp.FindPropertyRelative(nameof(TriggerParameter.SoundSource));
			//var colliderProp = parameterProp.FindPropertyRelative(nameof(TriggerParameter.Collider));

			//EditorGUI.BeginChangeCheck();
			//EditorGUI.indentLevel++;
			
			//EditorGUILayout.PropertyField(sourceProp);
   //         EditorGUILayout.PropertyField(colliderProp);

   //         EditorGUILayout.LabelField(_instruction.GetText(Instruction.SoundTrigger_PasteDefaultParameter), EditorStyles.centeredGreyMiniLabel);
   //         EditorGUI.indentLevel--;
   //         if (EditorGUI.EndChangeCheck())
			//{
			//	serializedObject.ApplyModifiedProperties();
			//}

            _reorderableList.DoLayoutList();
		}
    }
}
