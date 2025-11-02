using UnityEditor;
using System.IO;
using Ami.Extension;
using UnityEngine;
using Ami.BroAudio.Editor;

namespace Ami.BroAudio.Editor.Setting
{
    public class OutputPathPage : WizardPage
    {
        private BroInstructionHelper _instruction = new BroInstructionHelper();
        private bool _hasOutputAssetPath = false;
		private PreferencesDrawer _preferencesDrawer = null;
		private SerializedObject _runtimeSettingSO = null;
		private SerializedObject _editorSettingSO = null;

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
			_runtimeSettingSO = new SerializedObject(BroEditorUtility.RuntimeSetting);
			_editorSettingSO = new SerializedObject(BroEditorUtility.EditorSetting);
			_preferencesDrawer = new PreferencesDrawer(_runtimeSettingSO, _editorSettingSO, _instruction);
		}

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
			_runtimeSettingSO.Update();
			_editorSettingSO.Update();

			_preferencesDrawer.DrawAssetOutputPath(() => EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f)), _hasOutputAssetPath, _instruction,
                () => _hasOutputAssetPath = Directory.Exists(BroEditorUtility.AssetOutputPath));

			_editorSettingSO.ApplyModifiedProperties();
			_runtimeSettingSO.ApplyModifiedProperties();
			GUILayout.FlexibleSpace();
        }
    }
}
