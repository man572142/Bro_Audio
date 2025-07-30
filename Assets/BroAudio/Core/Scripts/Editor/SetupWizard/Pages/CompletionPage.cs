using System;
using UnityEditor;
using UnityEngine;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor.Setting
{
    public class CompletionPage : WizardPage
    {
        private struct StepSection : IDisposable
        {
            public StepSection(string title, string description)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
            }
            public void Dispose()
            {
                EditorGUILayout.EndVertical();
            }
        }
        
        private const string DocUrl = "https://man572142s-organization.gitbook.io/broaudio";
        private const float DemoSceneFieldWidth = 150f;
        private const float ButtonWidth = 200;
        private const float ButtonHeight = 30;

        private BroInstructionHelper _instruction = new BroInstructionHelper();
        
        public override string PageTitle => "Setup Complete!";
        public override string PageDescription => "Configured Successfully! You're now ready to start using BroAudio!";

        public override void DrawContent()
        {
            var buttonWidth = GUILayout.Width(ButtonWidth);
            var buttonHeight = GUILayout.Height(ButtonHeight);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Next Steps:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            using (new StepSection("1. Play the demo!", _instruction.GetText(Instruction.PlayDemo)))
            {
                DrawDemoSceneReferences();
            }
            EditorGUILayout.Space(10);

            using (new StepSection("2. Create your first audio asset", "Use the Library Manager to create and configure audio assets."))
            using (new CenterScope(false))
            {
                if (GUILayout.Button("Open Library Manager", buttonWidth, buttonHeight))
                {
                    // Open the Library Manager window
                    EditorApplication.ExecuteMenuItem("Tools/BroAudio/Library Manager");
                }
            }
            EditorGUILayout.Space(10);

            using (new StepSection("3. Check out the documentation", "For detailed guides and API references."))
            using (new CenterScope(false))
            {
                if (GUILayout.Button("View Documentation", buttonWidth, buttonHeight))
                {
                    Application.OpenURL(DocUrl);
                }
            }
        }

        private void DrawDemoSceneReferences()
        {
            var demoRefWidth = GUILayout.Width(DemoSceneFieldWidth);
            using (new CenterScope(false))
            {
                if (_instruction.DemoScene)
                {
                    EditorGUILayout.ObjectField(_instruction.DemoScene, typeof(SceneAsset), false, demoRefWidth);
                }
                
                if (_instruction.URPDemoScene)
                {
                    EditorGUILayout.ObjectField(_instruction.URPDemoScene, typeof(SceneAsset), false, demoRefWidth);
                }
            }
        }
    }
}
