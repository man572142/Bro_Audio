using UnityEditor;
using MiProduction.BroAudio.Editor;

public class AssetPostprocessorEditor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if(importedAssets.Length <= 0 || !EditorWindow.HasOpenInstances<ClipEditorWindow>())
		{
			return;
		}

		ClipEditorWindow window = EditorWindow.GetWindow(typeof(ClipEditorWindow)) as ClipEditorWindow;
		window.OnPostprocessAllAssets();
	}
}
