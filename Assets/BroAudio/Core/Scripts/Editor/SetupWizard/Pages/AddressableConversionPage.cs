#if PACKAGE_ADDRESSABLES
using UnityEngine;
using static UnityEditor.EditorGUILayout;

namespace Ami.BroAudio.Editor.Setting
{
    public class AddressableConversionPage : WizardPage
    {
        public override string PageTitle => "Addressables Conversion Options";
        public override string PageDescription => "Configure the default behavior when marking an entity as Addressables.";
        public override SetupDepth RequiredDepth => SetupDepth.Comprehensive;
        protected override (string Name, string Url)[] DocReferences { get; set; } =
        {
            ("#Addressables In Bro Audio", "https://man572142s-organization.gitbook.io/broaudio/core-features/addressables"),
            ("#Addressables", "https://docs.unity3d.com/6000.1/Documentation/Manual/com.unity.addressables.html"),
        };

        public override void DrawContent()
        {
            GUILayout.FlexibleSpace();
            Drawer.DrawAddressableNeverAskOptions(GetControlRect(), GetControlRect());
        }
    }
}
#endif