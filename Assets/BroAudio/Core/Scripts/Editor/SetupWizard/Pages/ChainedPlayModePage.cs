using UnityEngine;
using static UnityEditor.EditorGUILayout;

namespace Ami.BroAudio.Editor.Setting
{
    public class ChainedPlayModePage : WizardPage
    {
        public override string PageTitle => "Chained Play Mode";
        public override string PageDescription => "Set default loop behavior for chained playback.";
        public override SetupDepth RequiredDepth => SetupDepth.Advanced;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Chained Play Mode", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager/design-the-sound/chained-playback"),
            ("#Loop", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager/design-the-sound/seamless-loop"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            Drawer.DrawChainedPlayMode(GetControlRect(), GetControlRect(), GetControlRect());
        }
    }
}
