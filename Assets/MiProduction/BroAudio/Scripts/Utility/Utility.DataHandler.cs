using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using MiProduction.BroAudio.Asset.Core;

using System.Linq;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static void DeleteAsset(string assetGUID)
		{
			DeleteJsonDataByAsset(assetGUID);
			if(TryGetAsset(assetGUID, out var asset))
			{
				DeleteEnumFile(asset.AssetName);
			}
		}

		public static bool TryGetAsset(string assetGUID,out IAudioAsset asset)
		{
			asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGUID) , typeof(ScriptableObject)) as IAudioAsset;
			return asset != null;
		}


		
	}
}