#if BroAudio_DevOnly
using System;
using UnityEditor;
using Ami.BroAudio.Tools;
using System.Linq;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;

namespace Ami.BroAudio.Editor.DevTool
{
    public static class PackageExporter
    {
		[MenuItem(BroName.MenuItem_BroAudio + "Export Package", priority = DevToolsMenuIndex + 13)]
        public static void Export()
        {
			string[] allfilePaths = AssetDatabase.GetAllAssetPaths();
			allfilePaths = allfilePaths
				.Where(x => IsBroAudioAsset(x) && !IsExcludedFile(x))
				.ToArray();

			AssetDatabase.ExportPackage(allfilePaths, "BroAudio" + DateTime.Now.ToString("yy-MM-dd-HH-mm") + ".unitypackage", ExportPackageOptions.Interactive);
        }

		private static bool IsBroAudioAsset(string path)
		{
			return path[0] == 'A' && path.StartsWith("Assets/BroAudio", StringComparison.Ordinal);
		}

		private static bool IsExcludedFile(string path)
		{
			if (!path.EndsWith(".asset", System.StringComparison.Ordinal))
			{
				return false;
			}
			return path.Contains("BroRuntimeSetting.asset") || path.Contains("BroEditorSetting.asset") || path.Contains("BroAudioData.asset");
		}
	}
}
#endif
