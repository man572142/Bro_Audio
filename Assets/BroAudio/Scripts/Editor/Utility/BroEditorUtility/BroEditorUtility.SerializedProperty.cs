using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using Ami.BroAudio.Data;
using static Ami.Extension.EditorScriptingExtension;
using Ami.Extension;

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
            property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.Name))).stringValue = string.Empty;
            property.FindPropertyRelative(nameof(AudioEntity.Clips)).arraySize = 0;
            property.FindPropertyRelative(AudioEntity.NameOf.IsShowClipPreview).boolValue = false;
            property.FindPropertyRelative(AudioEntity.NameOf.MulticlipsPlayMode).enumValueIndex = 0;

            var delayProp = property.FindPropertyRelative(nameof(AudioEntity.Delay));
            if(delayProp != null)
			{
                delayProp.floatValue = 0f;
			}

            var loopPorp = property.FindPropertyRelative(nameof(AudioEntity.Loop));
            if(loopPorp != null)
			{
                loopPorp.boolValue = false;
			}
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

        public static SerializedProperty GetSpatialSettingsProperty(SerializedProperty sourceProp, SpatialPropertyType propertyType)
        {
            switch (propertyType)
            {
                case SpatialPropertyType.StereoPan:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.StereoPan));
                case SpatialPropertyType.DopplerLevel:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.DopplerLevel));
                case SpatialPropertyType.MinDistance:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.MinDistance));
                case SpatialPropertyType.MaxDistance:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.MaxDistance));
                case SpatialPropertyType.SpatialBlend:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.SpatialBlend));
                case SpatialPropertyType.ReverbZoneMix:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.ReverbZoneMix));
                case SpatialPropertyType.Spread:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.Spread));
                case SpatialPropertyType.CustomRolloff:
                    return sourceProp.FindPropertyRelative(nameof(SpatialSettings.CustomRolloff));
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
            }
            return null;
        }
    }
}