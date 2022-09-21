using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio
{
    [CustomEditor(typeof(MusicLibraryAsset))]
    public class MusicLibraryAssetEditor : Editor
    {
        // TODO: §ï¥Îªx«¬

        MusicLibraryAsset asset;
        private void OnEnable()
        {
            asset = target as MusicLibraryAsset;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (asset.Libraries != null && GUILayout.Button("Generate Enums"))
            {
                EnumGenerator.Generate("Music", asset.Libraries.Select(x => x.Name).ToArray());
            }
        }
    }

}