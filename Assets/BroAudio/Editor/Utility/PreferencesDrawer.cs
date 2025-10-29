using System;
using System.IO;
using System.Linq;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
    public class PreferencesDrawer
    {
        private readonly GUIContent _filterSlopeGUIContent, _playMusicAsBgmGUIContent, _showWarnForNoLoopChainedModeGUIContent,
            _updateModeGUIContent, _logAccessRecycledWarningGUIContent, _poolSizeCountGUIContent, _globalGroupGUIContent;
#if PACKAGE_ADDRESSABLES
        private readonly GUIContent _directToAddressableGUIContent, _addressableToDirectGUIContent;
#endif
        public readonly SerializedObject RuntimeSettingSO;
        public readonly SerializedObject EditorSettingSO;
        private SerializedProperty _soundIDDemoProp;

        public PreferencesDrawer(SerializedObject runtimeSettingSO, SerializedObject editorSettingSO, BroInstructionHelper instruction)
        {
            RuntimeSettingSO = runtimeSettingSO;
            EditorSettingSO = editorSettingSO;
            _filterSlopeGUIContent = new GUIContent("Audio Filter Slope", instruction.GetText(Instruction.AudioFilterSlope));
            _updateModeGUIContent = new GUIContent("Update Mode", instruction.GetText(Instruction.UpdateMode));
            _playMusicAsBgmGUIContent = new GUIContent("Always Play Music As BGM", instruction.GetText(Instruction.AlwaysPlayMusicAsBGM));
            _showWarnForNoLoopChainedModeGUIContent = new GUIContent("Show Warning If No Loop", "Displays a warning if the entity has no loop options set and will fall back to default settings");
            _logAccessRecycledWarningGUIContent = new GUIContent("Log Access Recycled Player Warning", instruction.GetText(Instruction.LogAccessRecycledWarning));
            _poolSizeCountGUIContent = new GUIContent("Audio Player Object Pool Size", instruction.GetText(Instruction.AudioPlayerPoolSize));
            _globalGroupGUIContent = new GUIContent("Global Playback Group", instruction.GetText(Instruction.GlobalPlaybackGroup));
#if PACKAGE_ADDRESSABLES
            string aaTooltip = instruction.GetText(Instruction.LibraryManager_AddressableConversionTooltip);
            _directToAddressableGUIContent = new GUIContent("Direct → Addressables", aaTooltip);
            _addressableToDirectGUIContent = new GUIContent("Addressables → Direct", aaTooltip); 
#endif
        }
        
        public void DrawAudioFilterSlope(Rect rect)
        {
            var filterSlopeProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.AudioFilterSlope));
            filterSlopeProp.enumValueIndex = (int)(FilterSlope)EditorGUI.EnumPopup(rect, _filterSlopeGUIContent, (FilterSlope)filterSlopeProp.enumValueIndex);
        }
        
        public void DrawUpdateMode(Rect rect)
        {
            var updateModeProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.UpdateMode));
            updateModeProp.enumValueIndex = (int)(AudioMixerUpdateMode)EditorGUI.EnumPopup(rect, _updateModeGUIContent, (AudioMixerUpdateMode)updateModeProp.enumValueIndex);
        }

        public void DrawGlobalPlaybackGroup(Rect rect)
        {
            var playbackGroupProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.GlobalPlaybackGroup));
            playbackGroupProp.objectReferenceValue = (PlaybackGroup)EditorGUI.ObjectField(rect, _globalGroupGUIContent, playbackGroupProp.objectReferenceValue, typeof(PlaybackGroup), false);
        }
        
        public void DrawBGMSetting(Rect toggleRect, Rect transitionRect, Rect transitionTimeRect)
        {
            var alwaysBGMProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.AlwaysPlayMusicAsBGM));
            var bgmTransitionProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultBGMTransition));
            var bgmTransitionTimeProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultBGMTransitionTime));
            alwaysBGMProp.boolValue = EditorGUI.Toggle(toggleRect, _playMusicAsBgmGUIContent, alwaysBGMProp.boolValue);

            if (alwaysBGMProp.boolValue)
            {
                bgmTransitionProp.enumValueIndex =
                    (int)(Transition)EditorGUI.EnumPopup(transitionRect, "Default Transition", (Transition)bgmTransitionProp.enumValueIndex);


                bgmTransitionTimeProp.floatValue = 
                    EditorGUI.FloatField(transitionTimeRect, "Default Transition Time", bgmTransitionTimeProp.floatValue);
            }
        }

        public void DrawChainedPlayMode(Rect popupRect, Rect transitionTimeRect, Rect warningToggleRect)
        {
            var loopTypeProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultChainedPlayModeLoop));
            var seamlessTransitionProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultChainedPlayModeTransitionTime));
            var showWarningProp = EditorSettingSO.FindProperty(nameof(EditorSetting.ShowWarningWhenEntityHasNoLoopInChainedMode));
            var loopType = (LoopType)EditorGUI.EnumPopup(popupRect, "Default Loop Type", (LoopType)loopTypeProp.enumValueIndex);
            loopTypeProp.enumValueIndex = (int)loopType;

            if (loopType == LoopType.SeamlessLoop)
            {
                seamlessTransitionProp.floatValue =
                    EditorGUI.FloatField(transitionTimeRect, "Default Transition Time", seamlessTransitionProp.floatValue);
            }

            showWarningProp.boolValue = EditorGUI.Toggle(warningToggleRect, _showWarnForNoLoopChainedModeGUIContent, showWarningProp.boolValue);

        }
        
        public void DrawAudioPlayerSetting(Rect accessRecycledWarnRect, Rect maxPoolSizeRect)
        {
            var logProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.LogAccessRecycledPlayerWarning));
            logProp.boolValue = EditorGUI.Toggle(accessRecycledWarnRect, _logAccessRecycledWarningGUIContent, logProp.boolValue);

            var poolSizeProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultAudioPlayerPoolSize));
            float fieldWidth = maxPoolSizeRect.width - EditorGUIUtility.labelWidth;
            maxPoolSizeRect.width -= fieldWidth - 50f;
            poolSizeProp.intValue = EditorGUI.IntField(maxPoolSizeRect, _poolSizeCountGUIContent, poolSizeProp.intValue);
        }

        public void DrawDefaultEasing(Rect fadeInRect, Rect fadeOutRect)
        {
            var fadeInProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultFadeInEase));
            var fadeOutProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultFadeOutEase));
            fadeInProp.enumValueIndex =
                (int)(Ease)EditorGUI.EnumPopup(fadeInRect, "Fade In", (Ease)fadeInProp.enumValueIndex);
            fadeOutProp.enumValueIndex =
                (int)(Ease)EditorGUI.EnumPopup(fadeOutRect, "Fade Out", (Ease)fadeOutProp.enumValueIndex);
        }

        public void DrawSeamlessLoopEasing(Rect fadeInRect, Rect fadeOutRect)
        {
            var fadeInProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.SeamlessFadeInEase));
            var fadeOutProp = RuntimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.SeamlessFadeOutEase));
            fadeInProp.enumValueIndex =
                (int)(Ease)EditorGUI.EnumPopup(fadeInRect, "Fade In", (Ease)fadeInProp.enumValueIndex);
            fadeOutProp.enumValueIndex =
                (int)(Ease)EditorGUI.EnumPopup(fadeOutRect, "Fade Out", (Ease)fadeOutProp.enumValueIndex);
        }

#if PACKAGE_ADDRESSABLES
        public void DrawAddressableNeverAskOptions(Rect directToAddressableRect, Rect addressableToDirectRect)
        {
            var directDecisionProp = EditorSettingSO.FindProperty(nameof(EditorSetting.DirectReferenceDecision));
            var addressableDecisionProp = EditorSettingSO.FindProperty(nameof(EditorSetting.AddressableDecision));
            DrawOption(directToAddressableRect, _directToAddressableGUIContent, directDecisionProp);
            DrawOption(addressableToDirectRect, _addressableToDirectGUIContent, addressableDecisionProp);
            
            void DrawOption(Rect rect, GUIContent label, SerializedProperty property)
            {
                SplitRectHorizontal(rect, 0.4f, 10f, out Rect labelRect, out Rect popupRect);
                EditorGUI.LabelField(labelRect, label);
                property.enumValueIndex = (int)(EditorSetting.ReferenceConversionDecision)EditorGUI.EnumPopup(popupRect, (EditorSetting.ReferenceConversionDecision)property.enumValueIndex);
            }
        }
#endif
        
        public void DrawAudioTypeDrawedProperties(Rect rect, float lineHeight, Action onDrawEmptyLine)
        {
            DrawTwoColumnAudioType(rect, lineHeight, DrawAudioTypeDrawedPropertiesField, onDrawEmptyLine);
            
            void DrawAudioTypeDrawedPropertiesField(Rect fieldRect, BroAudioType audioType)
            {
                if (BroEditorUtility.EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
                {
                    EditorGUI.BeginChangeCheck();
                    setting.DrawedProperty = (DrawedProperty)EditorGUI.EnumFlagsField(fieldRect, audioType.ToString(), setting.DrawedProperty);
                    if(EditorGUI.EndChangeCheck())
                    {
                        BroEditorUtility.EditorSetting.WriteAudioTypeSetting(audioType, setting);
                    }                  
                }
            }
        }

        public void DrawAudioTypeColorSettings(Rect rect, float lineHeight, Action onDrawEmptyLine)
        {
            DrawTwoColumnAudioType(rect, lineHeight, DrawAudioTypeLabelColorField, onDrawEmptyLine);
            
            void DrawAudioTypeLabelColorField(Rect fieldRect, BroAudioType audioType)
            {
                if(BroEditorUtility.EditorSetting.TryGetAudioTypeSetting(audioType,out var setting))
                {
                    EditorGUI.BeginChangeCheck();
                    setting.Color = EditorGUI.ColorField(fieldRect, audioType.ToString(), setting.Color);
                    if (EditorGUI.EndChangeCheck())
                    {
                        BroEditorUtility.EditorSetting.WriteAudioTypeSetting(audioType, setting);
                    }
                }
            }
        }
        
        public static void DrawTwoColumnAudioType(Rect rect, float lineHeight, Action<Rect, BroAudioType> onDraw, Action onDrawEmptyLine, float gap = 15f)
        {
            SplitRectHorizontal(rect, 0.5f, gap, out Rect leftRect, out Rect rightRect);
            int count = 0;
            Utility.ForeachConcreteAudioType((audioType) =>
            {
                onDraw?.Invoke(count % 2 == 0 ? leftRect : rightRect, audioType);
                count++;
                if (count % 2 == 1)
                {
                    leftRect.y += lineHeight;
                    rightRect.y += lineHeight;
                    onDrawEmptyLine?.Invoke();
                }
            });
        }
        
        public static void DemonstrateSlider(Rect sliderRect, bool isShow, ref float sliderValue)
        {
            if (isShow)
            {
                Rect vuRect = new Rect(sliderRect);
                vuRect.height *= 0.5f;
                EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent(IconConstant.HorizontalVUMeter).image);
                EditorGUI.DrawRect(vuRect, BroAudioGUISetting.VUMaskColor);
            }
            sliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, 0f, 1.25f);
        }

        public void DemonstrateSoundIDField(Rect fieldRect)
        {
            _soundIDDemoProp ??= GetSoundIDDemoSerializedProperty();
            EditorGUI.PropertyField(fieldRect, _soundIDDemoProp);
        }

        private SerializedProperty GetSoundIDDemoSerializedProperty()
        {
            SoundID id = default;

#pragma warning disable CS0618 // Type or member is obsolete
            if (BroEditorUtility.TryGetDemoData(out _, out var entity))
            {
                id = new SoundID(entity);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            var scriptableObject = ScriptableObject.CreateInstance<SoundIDDemonstration>();
            scriptableObject.Demonstration = new SoundID(entity);
            return new SerializedObject(scriptableObject).FindProperty(nameof(SoundIDDemonstration.Demonstration));
        }
    }

    public class SoundIDDemonstration : ScriptableObject
    {
        public SoundID Demonstration;
    }
}
