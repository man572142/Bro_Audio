using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public class EditPrefabAssetScope : IDisposable
    {

        public readonly string AssetPath;
        public readonly GameObject PrefabRoot;

        public EditPrefabAssetScope(string assetPath)
        {
            AssetPath = assetPath;
            PrefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        }

        public void Dispose()
        {
            PrefabUtility.SaveAsPrefabAsset(PrefabRoot, AssetPath);
            PrefabUtility.UnloadPrefabContents(PrefabRoot);
        }
    }
}