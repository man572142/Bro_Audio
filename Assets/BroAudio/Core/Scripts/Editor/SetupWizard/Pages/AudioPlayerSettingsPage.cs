using Ami.Extension;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

namespace Ami.BroAudio.Editor.Setting
{
    public class AudioPlayerSettingsPage : WizardPage
    {
        public override string PageTitle => "Audio Player";
        public override string PageDescription => "Configure audio player object pool settings";
        public override SetupDepth RequiredDepth => SetupDepth.Advanced;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Recycled Player", "https://man572142s-organization.gitbook.io/broaudio/core-features/audio-player#accessing-a-recycled-audioplayer"),
            ("#Object Pool", "https://man572142s-organization.gitbook.io/broaudio/core-features/audio-player#object-pool"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            using (new EditorScriptingExtension.LabelWidthScope(EditorGUIUtility.labelWidth * 1.65f))
            {
                Drawer.DrawAudioPlayerSetting(GetControlRect(), GetControlRect());
            }
        }
    }
}
