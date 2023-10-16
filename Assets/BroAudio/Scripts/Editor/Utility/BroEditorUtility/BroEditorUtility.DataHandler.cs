using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Editor
{
	public static partial class BroEditorUtility
	{
		public static void DeleteAssetRelativeData(string[] deletedAssetPaths)
		{
			if(deletedAssetPaths == null || deletedAssetPaths.Length == 0)
			{
				return;
			}

			DeleteJsonDataByAssetPath(deletedAssetPaths);

			string assetPath = AssetDatabase.GetAssetPath(Resources.Load(nameof(SoundManager)));
			using (var editScope = new EditPrefabAssetScope(assetPath))
			{
				if (editScope.PrefabRoot.TryGetComponent<SoundManager>(out var soundManager))
				{
					foreach (string path in deletedAssetPaths)
					{
						if (!string.IsNullOrEmpty(path) && TryGetAssetByPath(path, out var asset))
						{
							ScriptableObject scriptableObject = asset as ScriptableObject;
							soundManager.RemoveDeletedAsset(scriptableObject);
						}
						else
						{
							soundManager.RemoveDeletedAsset(null);
						}
					}
				}
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