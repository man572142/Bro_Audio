using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
    [CustomEditor(typeof(AudioLibraryAsset<IAudioLibrary>))]
    public class AudioLibraryAssetEditor : Editor
    {
        AudioLibraryAsset<IAudioLibrary> asset;
        private void OnEnable()
        {
            asset = target as AudioLibraryAsset<IAudioLibrary>;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (asset.Libraries != null && GUILayout.Button("Generate Enums"))
            {
                EnumGenerator.Generate("Music", asset.Libraries.Select(x => x.GetName()).ToArray());
            }
        }
    }

}