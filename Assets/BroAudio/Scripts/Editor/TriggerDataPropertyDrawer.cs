using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using UnityEditorInternal;
using System;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(TriggerData))]
	public class TriggerDataPropertyDrawer : MiPropertyDrawer
	{
        public const float ActionEventGap = 30f;
        public const float ParameterIndent = 5f;
        public const float ParameterLabelRatio = 0.35f;
        private readonly GUIContent _eventActionLabel = new GUIContent("On");

        // todo: using enum is a bad idea, use Reflection like Unity did with UnityAction.
        private BroAction[] _actionValues = null;
        private UnityMessage[] _unityMessageValues = null;

        public BroAction[] ActionsValues
        {
            get
            {
                if(_actionValues == null)
                {
                    _actionValues = (BroAction[])Enum.GetValues(typeof(BroAction));
                }
                return _actionValues;
            }
        }

        public UnityMessage[] UnityMessageValues
        {
            get
            {
                if(_unityMessageValues == null)
                {
                    _unityMessageValues = (UnityMessage[])Enum.GetValues(typeof(UnityMessage));
                }
                return _unityMessageValues;
            }
        }

        public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + ReorderableList.Defaults.padding * 0.5f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty actionProp = property.FindPropertyRelative(nameof(TriggerData.DoAction));
            BroAction broAction = ActionsValues[actionProp.enumValueIndex];
            float height = SingleLineSpace + ReorderableList.Defaults.padding *2;
            switch (broAction)
            {
                case BroAction.Play:
                case BroAction.StopById:
                case BroAction.StopByType:
                case BroAction.Pause:
                case BroAction.SetVolume:
                case BroAction.SetEffect:
                    return height + SingleLineSpace * 1;
                case BroAction.PlayFollowTarget:
                case BroAction.PlayInPosition:
                case BroAction.StopByIdFadeTime:
                case BroAction.StopByTypeFadeTime:
                case BroAction.PauseFadeTime:
                case BroAction.SetVolumeById:
                case BroAction.SetVolumeByType:
                case BroAction.SetEffectByType:
                    return height + SingleLineSpace * 2;
                case BroAction.SetVolumeByIdFadeTime:
                case BroAction.SetVolumeByTypeFadeTime:
                    return height + SingleLineSpace * 3;
            }
            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            position.height -= ReorderableList.Defaults.padding * 2;
            position.y += ReorderableList.Defaults.padding;
            DrawBackgroundWindow(position);

            Rect triggerRect = GetRectAndIterateLine(position);
            SplitRectHorizontal(triggerRect, 0.5f, ActionEventGap, out Rect actionRect, out Rect eventRect);
            SerializedProperty actionProp = property.FindPropertyRelative(nameof(TriggerData.DoAction));
            SerializedProperty eventProp = property.FindPropertyRelative(nameof(TriggerData.OnEvent));

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(actionRect, actionProp, GUIContent.none);
            EditorGUI.PropertyField(eventRect, eventProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            Rect gapLabel = new Rect(actionRect);
            gapLabel.width = ActionEventGap;
            gapLabel.x = actionRect.xMax;
            EditorGUI.LabelField(gapLabel, _eventActionLabel, GUIStyleHelper.MiddleCenterText);

            var paraProp = property.FindPropertyRelative(nameof(TriggerData.Parameter));
            int lineCountBeforeDrawing = DrawLineCount;
            actionRect.width -= ParameterIndent * 2;
            actionRect.x += ParameterIndent;
            OnDrawActionParameter(actionRect,paraProp, actionProp.enumValueIndex);
            DrawLineCount = lineCountBeforeDrawing;

        }

        private void DrawBackgroundWindow(Rect position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                SplitRectHorizontal(position, 0.5f, ActionEventGap, out Rect actionWindowRect, out Rect eventWindowRect);
                GUIStyle window = GUI.skin.window;
                window.Draw(actionWindowRect, false, false, false, false);
                window.Draw(eventWindowRect, false, false, false, false);
            }
        }

        private void OnDrawActionParameter(Rect position, SerializedProperty paraProp, int enumValueIndex)
        {
            EditorGUI.BeginChangeCheck();
            BroAction broAction = ActionsValues[enumValueIndex];

            // First parameter
            switch (broAction)
            {
                case BroAction.Play:
                case BroAction.PlayFollowTarget:
                case BroAction.PlayInPosition:
                case BroAction.StopById:
                case BroAction.StopByIdFadeTime:     
                case BroAction.Pause:
                case BroAction.PauseFadeTime:
                case BroAction.SetVolumeById:
                case BroAction.SetVolumeByIdFadeTime:
                    DrawProperty(paraProp.FindPropertyRelative(nameof(TriggerParameter.SoundContainer)), "Container");
                    break;
                case BroAction.StopByType:
                case BroAction.StopByTypeFadeTime:
                case BroAction.SetVolumeByType:
                case BroAction.SetVolumeByTypeFadeTime:
                case BroAction.SetEffectByType:
                    DrawProperty(paraProp.FindPropertyRelative(nameof(TriggerParameter.AudioType)), "AudioType");
                    break;
            }

            // Second parameter
            switch (broAction)
            {
                case BroAction.PlayFollowTarget:
                    string targetName = nameof(TriggerParameter.Target);
                    DrawProperty(paraProp.FindPropertyRelative(targetName), targetName);
                    break;
                case BroAction.PlayInPosition:
                    string positionName = nameof(TriggerParameter.Position);
                    DrawProperty(paraProp.FindPropertyRelative(positionName), positionName);
                    break;
                case BroAction.StopByIdFadeTime:
                case BroAction.StopByTypeFadeTime:
                case BroAction.PauseFadeTime:
                    DrawProperty(paraProp.FindPropertyRelative(nameof(TriggerParameter.FloatValue)), "FadeTime");
                    break;
                case BroAction.SetVolume:
                case BroAction.SetVolumeById:
                case BroAction.SetVolumeByIdFadeTime:
                case BroAction.SetVolumeByType:
                case BroAction.SetVolumeByTypeFadeTime:
                    DrawProperty(paraProp.FindPropertyRelative(nameof(TriggerParameter.FloatValue)),"Volume");              
                    break;
                case BroAction.SetEffect:
                case BroAction.SetEffectByType:
                    break;
            }

            // Third parameter
            switch (broAction)
            {
                case BroAction.SetVolumeByIdFadeTime:
                case BroAction.SetVolumeByTypeFadeTime:
                    DrawProperty(paraProp.FindPropertyRelative(nameof(TriggerParameter.FloatValue)), "FadeTime");
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                paraProp.serializedObject.ApplyModifiedProperties();
            }

            void DrawProperty(SerializedProperty property, string label)
            {
                Rect rect = GetRectAndIterateLine(position);
                SplitRectHorizontal(rect, ParameterLabelRatio, 0f, out Rect labelRect, out Rect propRect);
                EditorGUI.LabelField(labelRect, label);
                EditorGUI.PropertyField(propRect, property,GUIContent.none);
            }
        }
    }
}