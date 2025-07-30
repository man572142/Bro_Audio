using System;
using Ami.Extension;
using UnityEditor;
using UnityEngine.Audio;

namespace Ami.BroAudio.Editor.Setting
{
    public class UpdateModePage : WizardPage
    {
        public override string PageTitle => "Update Mode";
        public override string PageDescription => "Determines whether audio processing is affected by Time.timeScale.";

        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Update Mode", "https://man572142s-organization.gitbook.io/broaudio/core-features/customization#tab-audio"),
            ("#SetPitch()", "https://man572142s-organization.gitbook.io/broaudio/reference/api-documentation/class/broaudio#public-methods"),
        };

        public override void DrawContent()
        {
            EditorGUILayout.Space(50f);
            Drawer.DrawUpdateMode(EditorGUILayout.GetControlRect());

            switch (BroEditorUtility.RuntimeSetting.UpdateMode)
            {
                case AudioMixerUpdateMode.Normal:
                    EditorGUILayout.HelpBox("Audio processing is affected by time scale", MessageType.None);
                    EditorGUILayout.Space(10);
                    EditorScriptingExtension.RichTextHelpBox("Note: The pitch (speed) of the sound won't automatically adjust as time scale changes. " +
                                                             "To modify the pitch, consider using the <b>SetPitch()</b> API", MessageType.Info);
                    break;
                case AudioMixerUpdateMode.UnscaledTime:
                    EditorScriptingExtension.RichTextHelpBox("Audio processing is <b>NOT</b> affected by time scale", MessageType.None);
                    break;
            }
        }
    }
}
