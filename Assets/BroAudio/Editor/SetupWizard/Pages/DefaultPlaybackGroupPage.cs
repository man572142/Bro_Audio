using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class DefaultPlaybackGroupPage : WizardPage
    {
        private UnityEditor.Editor _editor;
        public override string PageTitle => "Default Playback Group";
        public override string PageDescription => "Configure the default PlaybackGroup settings.";
        public override SetupDepth RequiredDepth => SetupDepth.Advanced;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Playback Group", "https://man572142s-organization.gitbook.io/broaudio/core-features/playback-group"),
            ("#Max Playable Count", "https://man572142s-organization.gitbook.io/broaudio/core-features/playback-group#rule-and-value"),
            ("#Comb Filtering", "https://man572142s-organization.gitbook.io/broaudio/reference/technical-details#preventing-comb-filtering"),
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

        public override void OnDisable()
        {
            if (_editor)
            {
                Object.DestroyImmediate(_editor);
            }
        }

        public override void DrawContent()
        {
            EditorGUILayout.Space(5f);
            _editor.OnInspectorGUI();
            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox("These default rules apply when an entity doesn't have a PlaybackGroup. " +
                                    "Hover over a rule for a tooltip, or check the docs for more details.", MessageType.Info);
        }

        private void OnDestroy()
        {
            
        }
    }
}
