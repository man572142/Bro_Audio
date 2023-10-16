using UnityEditor;
using Ami.BroAudio.Editor;
using static Ami.Extension.EditorVersionAdapter;
using UnityEngine;
using System.Collections.Generic;

public class AssetPostprocessorEditor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		OnDeleteAssets(deletedAssets);
		OnReimportAsset(importedAssets);
	}

	private static void OnReimportAsset(string[] importedAssets)
	{
		if (importedAssets.Length > 0 && HasOpenEditorWindow<ClipEditorWindow>())
		{
			ClipEditorWindow window = EditorWindow.GetWindow(typeof(ClipEditorWindow)) as ClipEditorWindow;
			window.OnPostprocessAllAssets();
		}
	}

	private static void OnDeleteAssets(string[] deletedAssets)
	{
		if(deletedAssets != null && deletedAssets.Length > 0)
		{
			List<string> paths = new List<string>();
			foreach(string path in deletedAssets)
			{
				if(path.Contains(BroEditorUtility.AssetOutputPath))
				{
					paths.Add(path);
				}
			}

			if(paths.Count > 0)
			{
				BroEditorUtility.DeleteAssetRelativeData(deletedAssets);

				if (HasOpenEditorWindow<LibraryManagerWindow>())
				{
					LibraryManagerWindow editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;

					foreach (string path in deletedAssets)
					{
					editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(path));
					}
				}
			}
		}
	}
}
