using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(TriggerData))]
	public class TriggerDataPropertyDrawer : PropertyDrawer
	{
		private readonly GUIContent _titleLabel = new GUIContent("Action");
		private readonly GUIContent _eventLabel = new GUIContent("When");

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			//base.OnGUI(position, property, label);

			Rect suffixRect = EditorGUI.PrefixLabel(position, _titleLabel);
			SplitRectHorizontal(suffixRect, 3, 0f, out Rect[] rects);

			SerializedProperty actionProp = property.FindPropertyRelative(nameof(TriggerData.DoAction));
			actionProp.enumValueIndex = (int)(BroAction)EditorGUI.EnumPopup(rects[0], (BroAction)actionProp.enumValueIndex);

			EditorGUI.LabelField(rects[1], _eventLabel,GUIStyleHelper.MiddleCenterText);
			SerializedProperty eventProp = property.FindPropertyRelative(nameof(TriggerData.OnEvent));
			eventProp.enumValueIndex = (int)(SoundTriggerEvent)EditorGUI.EnumPopup(rects[2], (SoundTriggerEvent)eventProp.enumValueIndex);
		}
	}
}