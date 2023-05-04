using UnityEditor;

namespace MiProduction.BroAudio.AssetEditor
{
	public class AssetModificationEditor : UnityEditor.AssetModificationProcessor
	{
		public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
		{
			Utility.DeleteAssetRelativeData(assetPath);
			var editorWindow = EditorWindow.GetWindow(typeof(BroAudioEditorWindow)) as BroAudioEditorWindow;
			editorWindow.RemoveAssetEditor(AssetDatabase.AssetPathToGUID(assetPath));

			return AssetDeleteResult.DidNotDelete;
		}
	}
}