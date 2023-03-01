using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;
using MiProduction.BroAudio.Library.Core;

using System.Linq;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{

		public static void DeleteLibrary(string assetGUID)
		{
			DeleteJsonDataByAsset(assetGUID);
			if(TryGetAsset(assetGUID, out var asset))
			{
				DeleteEnumFile(asset.LibraryName);
			}
		}


		public static bool TryGetAsset(string assetGUID,out IAudioLibraryAsset asset)
		{
			asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGUID) , typeof(ScriptableObject)) as IAudioLibraryAsset;
			return asset != null;
		}
	}
}