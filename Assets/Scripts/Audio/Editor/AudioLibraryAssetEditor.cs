using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // 因為無法使用T,故使用interface
            var asset = target as IAudioLibraryIdentify;

            if (asset != null && GUILayout.Button("Generate Enums"))
            {
                if (asset.AllLibraryEnums == null)
                    return;

                if(asset.AllLibraryEnums.Length == 0)
                {
                    EnumGenerator.Generate(asset.LibraryTypeName, new string[0]);
                }
                else
                {
                    EnumGenerator.Generate(asset.LibraryTypeName, asset.AllLibraryEnums);
                }            
            }
        }
    }

}