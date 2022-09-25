using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Asset
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // 因為無法使用T,故使用interface
            var asset = target as IAudioLibraryIdentify;

            if (asset != null && asset.Libraries != null && GUILayout.Button("Generate Enums"))
            {
                if(asset.Libraries.Length == 0)
                {
                    EnumGenerator.Generate(asset.LibraryTypeName, new string[0]);
                }
                else
                {
                    EnumGenerator.Generate(asset.LibraryTypeName, asset.Libraries.Select(x => x.EnumName).ToArray());
                }

                
            }
        }
    }

}