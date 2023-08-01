using UnityEditor;
using Ami.BroAudio.Editor;

public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
{
	public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
	{
		bool hasDelete = BroEditorUtility.DeleteAssetRelativeData(assetPath);
		if (hasDelete && EditorWindow.HasOpenInstances<LibraryManagerWindow>())
		{
			var editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;
			editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(assetPath));
		}

		return AssetDeleteResult.DidNotDelete;
	}
}