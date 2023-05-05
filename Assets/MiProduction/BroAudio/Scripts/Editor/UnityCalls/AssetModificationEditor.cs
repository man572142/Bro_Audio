using UnityEditor;
using MiProduction.BroAudio.AssetEditor;
using MiProduction.BroAudio;

public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
{
	public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
	{
		Utility.DeleteAssetRelativeData(assetPath);
		if (EditorWindow.HasOpenInstances<BroAudioEditorWindow>())
		{
			var editorWindow = EditorWindow.GetWindow(typeof(BroAudioEditorWindow)) as BroAudioEditorWindow;
			editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(assetPath));
		}

		return AssetDeleteResult.DidNotDelete;
	}
}