using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class CompletionPage : WizardPage
    {
        private const string DocUrl = "https://man572142s-organization.gitbook.io/broaudio";
        public override string PageTitle => "Setup Complete!";
        public override string PageDescription => "Configured Successfully! You're now ready to start using BroAudio in your project!";

        public override void DrawContent()
        {
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField("Next Steps:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawNextStep("1. Create your first audio asset", 
                "Use the Library Manager to create and configure audio assets.");

            DrawNextStep("2. Add the BroAudioManager prefab to your scene", 
                "This prefab is required for BroAudio to work at runtime.");

            DrawNextStep("3. Check out the documentation", 
                "For detailed guides and API references.");

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Library Manager", GUILayout.Width(200), GUILayout.Height(30)))
            {
                // Open the Library Manager window
                EditorApplication.ExecuteMenuItem("Tools/BroAudio/Library Manager");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("View Documentation", GUILayout.Width(200)))
            {
                Application.OpenURL(DocUrl);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            GUIStyle footerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("You can access these settings again anytime through 'Tools > BroAudio > Preferences'.", footerStyle);
        }

        private void DrawNextStep(string title, string description)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}
