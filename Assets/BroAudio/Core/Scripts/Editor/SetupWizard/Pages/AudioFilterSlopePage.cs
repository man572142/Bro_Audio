using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class AudioFilterSlopePage : WizardPage
    {
        public override string PageTitle => "Audio Filter Slope";
        public override string PageDescription => "Configure the default attenuation per octave that the HighPass/LowPass applies to.";
        public override SetupDepth RequiredDepth => SetupDepth.Comprehensive;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Filter Design", "https://man572142s-organization.gitbook.io/broaudio/reference/technical-details#the-audio-filter-design-in-broaudio"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            Drawer.DrawAudioFilterSlope(EditorGUILayout.GetControlRect());
        }
    }
}
