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
                _actionValues = _actionValues ?? (BroAction[])Enum.GetValues(typeof(BroAction));
                return _actionValues;
            }
        }

        public UnityMessage[] UnityMessageValues
        {
            get
            {
                _unityMessageValues = _unityMessageValues ?? (UnityMessage[])Enum.GetValues(typeof(UnityMessage));
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
            
            actionRect.width -= ParameterIndent * 2;
            actionRect.x += ParameterIndent;
            eventRect.width -= ParameterIndent * 2;
            eventRect.x += ParameterIndent;
            
            int lineCountBeforeDrawing = DrawLineCount;
            DrawActionParameter(actionRect,paraProp, actionProp.enumValueIndex);
            DrawLineCount = lineCountBeforeDrawing;
            DrawEventParameter(eventRect, paraProp, eventProp.enumValueIndex);
            
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

        private void DrawActionParameter(Rect actionRect, SerializedProperty paraProp, int enumValueIndex)
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
                    DrawDefaultProperty(actionRect, paraProp.FindPropertyRelative(nameof(TriggerParameter.SoundSource)), TriggerParameterType.Source);
                    break;
                case BroAction.StopByType:
                case BroAction.StopByTypeFadeTime:
                case BroAction.SetVolumeByType:
                case BroAction.SetVolumeByTypeFadeTime:
                case BroAction.SetEffectByType:
                    Rect audioTypeRect = GetRectAndIterateLine(actionRect);
                    SerializedProperty audioTypeProp = paraProp.FindPropertyRelative(nameof(TriggerParameter.AudioType));
                    BroAudioType audioType = (BroAudioType)EditorGUI.EnumPopup(audioTypeRect, BroEditorUtility.GetAudioTypeByIndex(audioTypeProp.enumValueIndex));
                    audioTypeProp.enumValueIndex = audioType.GetSerializedEnumIndex();
                    break;
            }

            // Second parameter
            switch (broAction)
            {
                case BroAction.PlayFollowTarget:
                    DrawDefaultProperty(actionRect, paraProp.FindPropertyRelative(nameof(TriggerParameter.Target)), TriggerParameterType.Follow);
                    break;
                case BroAction.PlayInPosition:
                    DrawDefaultProperty(actionRect, paraProp.FindPropertyRelative(nameof(TriggerParameter.Position)), TriggerParameterType.Position);
                    break;
                case BroAction.StopByIdFadeTime:
                case BroAction.StopByTypeFadeTime:
                case BroAction.PauseFadeTime:
                    DrawDefaultProperty(actionRect, paraProp.FindPropertyRelative(nameof(TriggerParameter.FadeTime)), TriggerParameterType.FadeTime);
                    break;
                case BroAction.SetVolume:
                case BroAction.SetVolumeById:
                case BroAction.SetVolumeByIdFadeTime:
                case BroAction.SetVolumeByType:
                case BroAction.SetVolumeByTypeFadeTime:
                    var volProp = paraProp.FindPropertyRelative(nameof(TriggerParameter.Volume));
                    DrawDefaultProperty(actionRect, volProp, TriggerParameterType.Volume, "1 is Full Volume, and higher is a boost");
                    volProp.floatValue = Mathf.Clamp(volProp.floatValue, AudioConstant.MinVolume, AudioConstant.MaxVolume);
                    break;
                case BroAction.SetEffect:
                case BroAction.SetEffectByType:
                    // todo: effect parameter
                    break;
            }

            // Third parameter
            switch (broAction)
            {
                case BroAction.SetVolumeByIdFadeTime:
                case BroAction.SetVolumeByTypeFadeTime:
                    DrawDefaultProperty(actionRect, paraProp.FindPropertyRelative(nameof(TriggerParameter.FadeTime)), TriggerParameterType.FadeTime);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                paraProp.serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawDefaultProperty(Rect position,SerializedProperty property, TriggerParameterType parameterType, string tooltip = null)
        {
            using (var labelWidthScope = new LabelWidthScope(position.width * ParameterLabelRatio))
            {
                Rect rect = GetRectAndIterateLine(position);
                EditorGUI.PropertyField(rect, property, new GUIContent(parameterType.ToString(), tooltip));
            }       
        }

        private void DrawEventParameter(Rect eventRect, SerializedProperty paraProp, int enumValueIndex)
        {
            EditorGUI.BeginChangeCheck();
            UnityMessage unityMessage = UnityMessageValues[enumValueIndex];

            switch (unityMessage)
            {
                case UnityMessage.Awake:
                case UnityMessage.Start:
                case UnityMessage.OnEnable:
                case UnityMessage.OnDisable:
                case UnityMessage.OnDestroy:
                    break;
                case UnityMessage.Update:
                case UnityMessage.FixedUpdate:
                case UnityMessage.LateUpdate:
                case UnityMessage.OnTriggerEnter:
                case UnityMessage.OnTriggerStay:
                case UnityMessage.OnTriggerExit:
                case UnityMessage.OnCollisionEnter:
                case UnityMessage.OnCollisionStay:
                case UnityMessage.OnCollisionExit:
                case UnityMessage.OnTriggerEnter2D:
                case UnityMessage.OnTriggerStay2D:
                case UnityMessage.OnTriggerExit2D:
                case UnityMessage.OnCollisionEnter2D:
                case UnityMessage.OnCollisionStay2D:
                case UnityMessage.OnCollisionExit2D:
                    Rect triggerOnceRect = GetRectAndIterateLine(eventRect);
                    var triggerOnceProp = paraProp.FindPropertyRelative(nameof(TriggerParameter.OnlyTriggerOnce));
                    triggerOnceProp.boolValue = EditorGUI.ToggleLeft(triggerOnceRect, TriggerParameterType.OnlyTriggerOnce.ToString(), triggerOnceProp.boolValue);
                    break;
            }


            if (EditorGUI.EndChangeCheck())
            {
                paraProp.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}