using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Ami.BroAudio.Data;
using static Ami.Extension.EditorScriptingExtension;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
    {
        public static void ResetBroAudioClipSerializedProperties(SerializedProperty property)
		{
			property.FindPropertyRelative(nameof(BroAudioClip.AudioClip)).objectReferenceValue = null;
			property.FindPropertyRelative(nameof(BroAudioClip.Weight)).intValue = 0;
			ResetBroAudioClipPlaybackSetting(property);
		}

		public static void ResetBroAudioClipPlaybackSetting(SerializedProperty property)
		{
			property.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = AudioConstant.FullVolume;
			property.FindPropertyRelative(nameof(BroAudioClip.StartPosition)).floatValue = 0f;
			property.FindPropertyRelative(nameof(BroAudioClip.EndPosition)).floatValue = 0f;
			property.FindPropertyRelative(nameof(BroAudioClip.FadeIn)).floatValue = 0f;
			property.FindPropertyRelative(nameof(BroAudioClip.FadeOut)).floatValue = 0f;
		}

		public static void ResetEntitySerializedProperties(SerializedProperty property)
        {
            //could use enumerator to improve this, but might have to deal with some property
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Name))).stringValue = string.Empty;
            property.FindPropertyRelative(nameof(AudioEntity.Clips)).arraySize = 0;
            property.FindPropertyRelative(AudioEntity.EditorPropertyName.MulticlipsPlayMode).enumValueIndex = 0;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.MasterVolume))).floatValue = AudioConstant.FullVolume;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Loop))).boolValue = false;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SeamlessLoop))).boolValue = false;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Pitch))).floatValue = AudioConstant.DefaultPitch;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.PitchRandomRange))).floatValue = 0f;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.RandomFlags))).intValue = 0;
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Priority))).intValue = AudioConstant.DefaultPriority;

            SerializedProperty spatialProp = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SpatialSetting)));
			spatialProp.objectReferenceValue = null;
            spatialProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		public static int GetSerializedEnumIndex(this BroAudioType audioType)
		{
			int index = 0;
			int intAudioType = (int)audioType;
			while (intAudioType > 0)
			{
				index++;
				intAudioType = intAudioType >> 1;
			}
			return index;
		}

        public static BroAudioType GetAudioTypeByIndex(int enumIndex)
        {
            BroAudioType audioType = BroAudioType.None;
            while(enumIndex > 0)
            {
                audioType = audioType.ToNext();
                enumIndex--;
            }
            return audioType;
        }

        public static void SafeSetCurve(this SerializedProperty property, AnimationCurve curve)
        {
            if (curve != null && curve.keys.Length > 0)
            {
                property.animationCurveValue = curve;
            }
        }
	}
}