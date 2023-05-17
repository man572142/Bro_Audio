using UnityEditor;
using UnityEngine;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static void DeleteAssetRelativeData(string assetPath)
		{
			bool hasDeleted = DeleteJsonDataByAsset(AssetDatabase.AssetPathToGUID(assetPath));
			if(hasDeleted && TryGetAssetByPath(assetPath, out var asset))
			{
				//DeleteEnumFile(asset.AssetName);
				ScriptableObject scriptableObject = asset as ScriptableObject;
				RemoveDeletedAssetFromSoundManager(scriptableObject);
			}
		}

		public static void AddToSoundManager(ScriptableObject asset)
		{
			string assetPath = AssetDatabase.GetAssetPath(Resources.Load(nameof(SoundManager)));
			using (var editScope = new EditPrefabAssetScope(assetPath))
			{
				if(editScope.PrefabRoot.TryGetComponent<SoundManager>(out var soundManager))
				{
					soundManager.AddAsset(asset);
				}
				editScope.PrefabRoot.transform.position = Vector3.one;
			}
		}

		public static void RemoveDeletedAssetFromSoundManager(ScriptableObject asset)
		{
			string assetPath = AssetDatabase.GetAssetPath(Resources.Load(nameof(SoundManager)));
			using (var editScope = new EditPrefabAssetScope(assetPath))
			{
				if (editScope.PrefabRoot.TryGetComponent<SoundManager>(out var soundManager))
				{
					soundManager.RemoveDeletedAsset(asset);
				}
			}
		}


		public static bool TryGetAssetByGUID(string assetGUID,out IAudioAsset asset)
		{
			return TryGetAssetByPath(AssetDatabase.GUIDToAssetPath(assetGUID) ,out asset);
		}

		public static bool TryGetAssetByPath(string assetPath, out IAudioAsset asset)
		{
			asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ScriptableObject)) as IAudioAsset;
			return asset != null;
		}
	}
}