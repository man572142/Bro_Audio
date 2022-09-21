using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MiProduction.BroAudio
{
	[CustomEditor(typeof(SoundLibraryAsset))]
	public class SoundLibraryAssetEditor : Editor
	{
        // TODO: §ï¥Îªx«¬

        SoundLibraryAsset asset;

        private void OnEnable()
        {
            asset = target as SoundLibraryAsset;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
			if(GUILayout.Button("Generate Enums"))
            {
                EnumGenerator.Generate("Sound", asset.Libraries.Select(x => x.Name).ToArray());
            }
        }
    } 
}