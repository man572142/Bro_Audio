using UnityEditor;
using UnityEngine;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.AssetEditor
{
    public class SerializedBroAudioClip : BroAudioClip
    {
        public static void ResetAllSerializedProperties(SerializedProperty property)
        {
            property.FindPropertyRelative(nameof(OriginAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(EditedAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(Volume)).floatValue = 1f;
            property.FindPropertyRelative(nameof(StartPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(EndPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(FadeIn)).floatValue = 0f;
            property.FindPropertyRelative(nameof(FadeOut)).floatValue = 0f;

            property.FindPropertyRelative(nameof(Weight)).intValue = 0;
        }
    }
}