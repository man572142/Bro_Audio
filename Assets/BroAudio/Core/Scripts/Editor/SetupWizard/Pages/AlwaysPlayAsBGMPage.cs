using Ami.Extension;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUILayout;

namespace Ami.BroAudio.Editor.Setting
{
    public class AlwaysPlayAsBGMPage : WizardPage
    {
        public override string PageTitle => "BGM Setting";
        public override string PageDescription => "Configure whether an entity of AudioType.Music should always be played as BGM.";
        
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#As BGM", "https://man572142s-organization.gitbook.io/broaudio/core-features/audio-player/music-player"),
            ("#Audio Type", "https://man572142s-organization.gitbook.io/broaudio/reference/api-documentation/enums/broaudiotype"),
            ("#Audio Entity", "https://man572142s-organization.gitbook.io/broaudio/core-features/library-manager#entity"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            using (new EditorScriptingExtension.LabelWidthScope(EditorGUIUtility.labelWidth * 1.2f))
            {
                Drawer.DrawBGMSetting(GetControlRect(), GetControlRect(), GetControlRect());
            }
        }
    }
}
