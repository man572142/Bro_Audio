using Ami.BroAudio.Data;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class GlobalPlaybackGroupPage : WizardPage
    {
        private UnityEditor.Editor _editor;
        public override string PageTitle => "Default Playback Group";
        public override string PageDescription => "Configure the default PlaybackGroup settings.";
        public override SetupDepth RequiredDepth => SetupDepth.Advanced;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Playback Group", "https://man572142s-organization.gitbook.io/broaudio/core-features/playback-group"),
            ("#Max Playable Count", "https://man572142s-organization.gitbook.io/broaudio/core-features/playback-group#rule-and-value"),
            ("#Comb Filtering", "https://man572142s-organization.gitbook.io/broaudio/reference/audio-terminology#comb-filtering"),
        };

        public override void OnEnable()
        {
            base.OnEnable();
            _editor = UnityEditor.Editor.CreateEditor(BroEditorUtility.RuntimeSetting.GlobalPlaybackGroup, typeof(PlaybackGroupEditor));
            if (_editor is PlaybackGroupEditor playbackGroupEditor)
            {
                playbackGroupEditor.OffsetWidth = SetupWizardWindow.WindowPadding * 2;
            }
        }

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            _editor.OnInspectorGUI();
        }
    }
}
