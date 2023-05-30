using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.EditorScriptingExtension;

namespace MiProduction.BroAudio.Editor
{
	public static partial class BroEditorUtility
    {
        public static void ResetBroAudioClipSerializedProperties(SerializedProperty property)
        {
            property.FindPropertyRelative(nameof(BroAudioClip.OriginAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(BroAudioClip.EditedAudioClip)).objectReferenceValue = null;
            property.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = 1f;
            property.FindPropertyRelative(nameof(BroAudioClip.StartPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(BroAudioClip.EndPosition)).floatValue = 0f;
            property.FindPropertyRelative(nameof(BroAudioClip.FadeIn)).floatValue = 0f;
            property.FindPropertyRelative(nameof(BroAudioClip.FadeOut)).floatValue = 0f;

            property.FindPropertyRelative(nameof(BroAudioClip.Weight)).intValue = 0;
        }

        public static void ResetLibrarySerializedProperties(SerializedProperty property)
        {
            property.FindPropertyRelative(GetAutoBackingFieldName(nameof(AudioLibrary.Name))).stringValue = string.Empty;
            property.FindPropertyRelative(nameof(AudioLibrary.Clips)).arraySize = 0;
            property.FindPropertyRelative(nameof(AudioLibrary.IsShowClipPreview)).boolValue = false;
            property.FindPropertyRelative(nameof(AudioLibrary.MulticlipsPlayMode)).enumValueIndex = 0;

            var delayProp = property.FindPropertyRelative(nameof(SoundLibrary.Delay));
            if(delayProp != null)
			{
                delayProp.floatValue = 0f;
			}

            var loopPorp = property.FindPropertyRelative(nameof(MusicLibrary.Loop));
            if(loopPorp != null)
			{
                loopPorp.boolValue = false;
			}
        }

	}
}