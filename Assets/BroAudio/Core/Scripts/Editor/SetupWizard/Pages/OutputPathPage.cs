using UnityEditor;
using System.IO;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
    public class OutputPathPage : WizardPage
    {
        private BroInstructionHelper _instruction = new BroInstructionHelper();
        private bool _hasOutputAssetPath = false;
        
        public override string PageTitle => "Output Path";
        public override string PageDescription => "Configure where your audio assets will be stored.";
        public override SetupDepth RequiredDepth => SetupDepth.Advanced;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Audio Asset", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager#asset"),
        };

        public override void OnEnable()
        {
            base.OnEnable();
            _hasOutputAssetPath = Directory.Exists(BroEditorUtility.AssetOutputPath);
        }

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            if (!_hasOutputAssetPath)
            {
                var warningRect = EditorGUILayout.GetControlRect();
                EditorScriptingExtension.RichTextHelpBox(warningRect, PreferencesEditorWindow.AssetOutputPathMissing, MessageType.Error);
                EditorGUILayout.Space(10);
            }
            
            BroEditorUtility.DrawAssetOutputPath(EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f)), _instruction, () => _hasOutputAssetPath = true);
        }
    }
}
