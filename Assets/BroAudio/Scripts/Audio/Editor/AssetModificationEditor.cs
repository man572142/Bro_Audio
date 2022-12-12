using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library
{
	public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
		{
			// delete from json
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			Utility.DeleteDataByAsset(guid);

			return AssetDeleteResult.DidNotDelete;
		}
	}

}