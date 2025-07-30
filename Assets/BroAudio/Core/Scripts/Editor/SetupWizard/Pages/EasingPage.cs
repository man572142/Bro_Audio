using UnityEngine;
using static UnityEditor.EditorGUILayout;

namespace Ami.BroAudio.Editor.Setting
{
    public class EasingPage : WizardPage
    {
        public override string PageTitle => "Transition Easing";
        public override string PageDescription => "Choose the default easing curve for fade‑ins and fade‑outs.";
        public override SetupDepth RequiredDepth => SetupDepth.Comprehensive;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Fading", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager/design-the-sound/fade-in-out-and-cross-fade"),
            ("#Seamless Loop", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager/design-the-sound/seamless-loop#seamless-loop"),
            ("#Easing Functions", "https://easings.net/"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            using (Section.NewSection("Default Easing", GetControlRect()))
            {
                Drawer.DrawDefaultEasing(GetControlRect(), GetControlRect());
            }

            using (Section.NewSection("Seamless Loop Easing", GetControlRect()))
            {
                Drawer.DrawSeamlessLoopEasing(GetControlRect(), GetControlRect());
            }
        }
    }
}
