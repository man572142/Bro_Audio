using UnityEditor;
using Ami.BroAudio.Editor;
using static Ami.Extension.EditorVersionAdapter;

public class AssetPostprocessorEditor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if(importedAssets.Length <= 0 || !HasOpenEditorWindow<ClipEditorWindow>())
		{
			return;
		}

		ClipEditorWindow window = EditorWindow.GetWindow(typeof(ClipEditorWindow)) as ClipEditorWindow;
		window.OnPostprocessAllAssets();
	}
}
