using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using UnityEditorInternal;
using System;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
	[CustomEditor(typeof(SoundTrigger))]
	public class SoundTriggerEditor : UnityEditor.Editor
	{
		private readonly GUIContent _eventActionLabel = new GUIContent("On");

		private ReorderableList _reorderableList = null;

		private void OnEnable()
		{
			InitReorderableList();
		}

		private void InitReorderableList()
		{
			SerializedProperty settingsProp = serializedObject.FindProperty(SoundTrigger.NameOf.TriggerSettings);
			_reorderableList = new ReorderableList(serializedObject, settingsProp)
			{
				drawHeaderCallback = OnDrawHeader,
				drawElementCallback = OnDrawElement,
			};

			// Hack: temp height
			_reorderableList.elementHeight = EditorGUIUtility.singleLineHeight * 5f;

			void OnDrawHeader(Rect rect)
			{
				EditorGUI.LabelField(rect,settingsProp.displayName);
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				rect.height -= ReorderableList.Defaults.padding * 2;
				rect.y += ReorderableList.Defaults.padding;

				SerializedProperty elementProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);

				float gap = 30f;
				SplitRectHorizontal(rect, 0.5f, gap, out Rect actionRect, out Rect eventRect);

				if(Event.current.type == EventType.Repaint)
				{
					GUIStyle window = GUI.skin.window;
					window.Draw(actionRect, false, false, false, false);
					window.Draw(eventRect, false, false, false, false);
				}
				
				SerializedProperty actionProp = elementProp.FindPropertyRelative(nameof(TriggerData.DoAction));
				SerializedProperty eventProp = elementProp.FindPropertyRelative(nameof(TriggerData.OnEvent));

				EditorGUI.BeginChangeCheck();
				actionProp.enumValueIndex =  (int)(BroAction)EditorGUI.EnumPopup(actionRect, (BroAction)actionProp.enumValueIndex);		
				eventProp.enumValueIndex = (int)(SoundTriggerEvent)EditorGUI.EnumPopup(eventRect, (SoundTriggerEvent)eventProp.enumValueIndex);
				if(EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
				}

				Rect gapLabel = new Rect(actionRect);
				gapLabel.width = gap;
				gapLabel.x = actionRect.xMax;
				EditorGUI.LabelField(gapLabel, _eventActionLabel,GUIStyleHelper.MiddleCenterText);
			}
		}

		public override void OnInspectorGUI()
		{
			SerializedProperty entityProp = serializedObject.FindProperty(SoundTrigger.NameOf.Sound);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField(SoundTrigger.Header_Entity, EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(entityProp);

			EditorGUILayout.Space();
			_reorderableList.DoLayoutList();
		}
	} 
}
