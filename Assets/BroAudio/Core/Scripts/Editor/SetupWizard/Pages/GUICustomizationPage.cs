using UnityEditor;

namespace Ami.BroAudio.Editor.Setting
{
    public class GUICustomizationPage : WizardPage
    {
        private float _demoSliderValue = 1f;
        public override string PageTitle => "GUI Customization";
        public override string PageDescription => "Determines whether to display some additional information or controls";
        public override SetupDepth RequiredDepth => SetupDepth.Comprehensive;

        public override void DrawContent()
        {
            EditorGUILayout.Space(20f);
            
            var editorSO = Drawer.EditorSettingSO;
            var showVuProp = editorSO.FindProperty(nameof(EditorSetting.ShowVUColorOnVolumeSlider));
            var showMasterProp = editorSO.FindProperty(nameof(EditorSetting.ShowMasterVolumeOnClipListHeader));
            var showAudioTypeProp = editorSO.FindProperty(nameof(EditorSetting.ShowAudioTypeOnSoundID));
            var showPlayButtonWhenCollapsed = editorSO.FindProperty(nameof(EditorSetting.ShowPlayButtonWhenEntityCollapsed));
            var openLastEditedAssetProp = editorSO.FindProperty(nameof(EditorSetting.OpenLastEditAudioAsset));
            
            showVuProp.boolValue = EditorGUILayout.ToggleLeft(PreferencesEditorWindow.VUColorToggleLabel, showVuProp.boolValue);
            PreferencesDrawer.DemonstrateSlider(EditorGUILayout.GetControlRect(), showVuProp.boolValue, ref _demoSliderValue);

            showMasterProp.boolValue = EditorGUILayout.ToggleLeft(PreferencesEditorWindow.ShowMasterVolumeLabel, showMasterProp.boolValue);
            showPlayButtonWhenCollapsed.boolValue = EditorGUILayout.ToggleLeft(PreferencesEditorWindow.ShowPlayButtonWhenCollapsed, showPlayButtonWhenCollapsed.boolValue);
            openLastEditedAssetProp.boolValue = EditorGUILayout.ToggleLeft(PreferencesEditorWindow.OpenLastEditedAssetLabel, openLastEditedAssetProp.boolValue);
            
            EditorGUILayout.Space();
            DrawOpenLibraryManagerButton(false);
            EditorGUILayout.Space();
            
            showAudioTypeProp.boolValue = EditorGUILayout.ToggleLeft(PreferencesEditorWindow.ShowAudioTypeToggleLabel, showAudioTypeProp.boolValue);
            Drawer.DemonstrateSoundIDField(EditorGUILayout.GetControlRect());
        }
    }
}
