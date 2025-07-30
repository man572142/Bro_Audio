using System;
using Ami.Extension;
using UnityEditor;
using UnityEngine.Audio;

namespace Ami.BroAudio.Editor.Setting
{
    public class UpdateModePage : WizardPage
    {
        public override string PageTitle => "Update Mode";
        public override string PageDescription => "Choose how Bro Audio’s processing reacts to Unity’s time scale.";

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
                    EditorGUILayout.HelpBox("Audio follows Time.timeScale.", MessageType.None);
                    EditorGUILayout.Space(10);
                    EditorScriptingExtension.RichTextHelpBox("Note: Pitch (playback speed) doesn't change automatically when you adjust time scale. Use the <b>SetPitch()</b> API if you need dynamic pitch control.", MessageType.Info);
                    break;
                case AudioMixerUpdateMode.UnscaledTime:
                    EditorGUILayout.HelpBox("Audio ignores Time.timeScale and always runs at real‑time speed.", MessageType.None);
                    break;
            }
        }
    }
}
