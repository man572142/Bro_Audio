using UnityEditor;
using MiProduction.BroAudio.Editor;
using MiProduction.BroAudio;

public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
{
	public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
	{
		Utility.DeleteAssetRelativeData(assetPath);
		if (EditorWindow.HasOpenInstances<LibraryManagerWindow>())
		{
			var editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;
			editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(assetPath));
		}

		return AssetDeleteResult.DidNotDelete;
	}
}