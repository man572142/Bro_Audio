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
            // �]���L�k�ϥ�T,�G�ϥ�interface
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