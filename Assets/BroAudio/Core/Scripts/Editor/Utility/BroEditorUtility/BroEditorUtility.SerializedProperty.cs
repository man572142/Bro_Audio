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

            SerializedProperty spatialProp = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.SpatialSettings)));
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.StereoPan)).floatValue = 0f;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.DopplerLevel)).floatValue = AudioConstant.DefaultDoppler;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.MinDistance)).floatValue = AudioConstant.AttenuationMinDistance;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.MaxDistance)).floatValue = AudioConstant.AttenuationMaxDistance;

            spatialProp.FindPropertyRelative(nameof(SpatialSettings.SpatialBlend)).animationCurveValue = BroEditorUtility.SpatialBlend;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.ReverbZoneMix)).animationCurveValue = BroEditorUtility.ReverbZoneMix;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.Spread)).animationCurveValue = BroEditorUtility.Spread;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.CustomRolloff)).animationCurveValue = null;
            spatialProp.FindPropertyRelative(nameof(SpatialSettings.RolloffMode)).enumValueIndex = (int)AudioRolloffMode.Logarithmic;
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

        public static SerializedProperty GetSpatialSettingsProperty(SerializedProperty settingProp, SpatialPropertyType propertyType)
        {
            switch (propertyType)
            {
                case SpatialPropertyType.StereoPan:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.StereoPan));
                case SpatialPropertyType.DopplerLevel:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.DopplerLevel));
                case SpatialPropertyType.MinDistance:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.MinDistance));
                case SpatialPropertyType.MaxDistance:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.MaxDistance));
                case SpatialPropertyType.SpatialBlend:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.SpatialBlend));
                case SpatialPropertyType.ReverbZoneMix:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.ReverbZoneMix));
                case SpatialPropertyType.Spread:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.Spread));
                case SpatialPropertyType.CustomRolloff:
                    return settingProp.FindPropertyRelative(nameof(SpatialSettings.CustomRolloff));
                case SpatialPropertyType.RolloffMode:
                    return settingProp.FindPropertyRelative(nameof(SpatialPropertyType.RolloffMode));
            }
            return null;
        }

        public static SerializedProperty GetAudioSourceProperty(SerializedObject sourceSO, SpatialPropertyType propertyType)
        {
            switch (propertyType)
            {
                case SpatialPropertyType.StereoPan:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.StereoPan);
                case SpatialPropertyType.DopplerLevel:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.DopplerLevel);
                case SpatialPropertyType.MinDistance:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.MinDistance);
                case SpatialPropertyType.MaxDistance:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.MaxDistance);
                case SpatialPropertyType.SpatialBlend:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.SpatialBlend);
                case SpatialPropertyType.ReverbZoneMix:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.ReverbZoneMix);
                case SpatialPropertyType.Spread:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.Spread);
                case SpatialPropertyType.CustomRolloff:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.CustomRolloff);
                case SpatialPropertyType.RolloffMode:
                    return sourceSO.FindProperty(AudioSourcePropertyPath.RolloffMode);
            }
            return null;
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