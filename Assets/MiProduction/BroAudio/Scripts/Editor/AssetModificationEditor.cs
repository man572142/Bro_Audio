using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio.AssetEditor
{
	public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
		{
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			Utility.DeleteAsset(guid);

			return AssetDeleteResult.DidNotDelete;
		}
	}

}