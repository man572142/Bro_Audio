using Ami.Extension;
using UnityEditor;

namespace Ami.BroAudio.Editor.Setting
{
    public class DisplayedPropertiesPage : WizardPage
    {
        public override string PageTitle => "Displayed Properties";
        public override string PageDescription => "Select which properties appear in the Library Manager.";
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Displayed Properties", "https://man572142s-organization.gitbook.io/broaudio/core-features/customization#displayed-properties"),
        };

        public override void DrawContent()
        {
            EditorGUILayout.Space(20f);
            using (new EditorScriptingExtension.LabelWidthScope(EditorGUIUtility.labelWidth * 0.5f))
            {
                Drawer.DrawAudioTypeDrawedProperties(EditorGUILayout.GetControlRect(), EditorGUIUtility.singleLineHeight * 1.2f, EditorGUILayout.Space);
            }
            EditorGUILayout.Space(30f);


            DrawOpenLibraryManagerButton(true);
            EditorGUILayout.HelpBox("You can also toggle these properties anytime by clicking an entity’s dropdown in the Library Manager", MessageType.Info);
        }
    }
}
