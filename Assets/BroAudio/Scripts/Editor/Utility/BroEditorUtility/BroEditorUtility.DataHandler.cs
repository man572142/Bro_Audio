using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
// For versions that under Unity 2019.2
using static Ami.Extension.VersionAdapter;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		public static bool DeleteAssetRelativeData(string assetPath)
		{
			bool hasDeleted = DeleteJsonDataByAsset(AssetDatabase.AssetPathToGUID(assetPath));
			if(hasDeleted && TryGetAssetByPath(assetPath, out var asset))
			{
				ScriptableObject scriptableObject = asset as ScriptableObject;
				RemoveDeletedAssetFromSoundManager(scriptableObject);
			}
			return hasDeleted;
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