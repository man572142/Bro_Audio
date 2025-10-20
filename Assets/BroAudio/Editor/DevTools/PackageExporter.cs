#if BroAudio_DevOnly
using System;
using UnityEditor;
using Ami.BroAudio.Tools;
using System.Linq;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using Ami.BroAudio.Data;
using System.Collections.Generic;
using System.IO;

namespace Ami.BroAudio.Editor.DevTool
{
    public static class PackageExporter
    {
		[MenuItem(BroName.MenuItem_BroAudio + "Export Package", priority = DevToolsMenuIndex + 13)]
        public static void Export()
        {
			List<string> allfilePaths = Directory.GetFiles("Assets/BroAudio", "*.*", SearchOption.AllDirectories)
                .Select(x => x.Replace("\\", "/"))
                .ToList();

            allfilePaths.RemoveAll(x => x.EndsWith($"{BroName.RuntimeSettingName}.asset", StringComparison.OrdinalIgnoreCase) ||
                                        x.EndsWith($"{BroName.EditorSettingName}.asset", StringComparison.OrdinalIgnoreCase) ||
                                        x.EndsWith($"{BroName.CoreDataName}.asset", StringComparison.OrdinalIgnoreCase) ||
                                        x.EndsWith($"{BroName.GlobalPlaybackGroupName}.asset", StringComparison.OrdinalIgnoreCase) ||
                                        x.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));

            if(EditorUtility.DisplayDialog("Export BroAudio Package", $"Export Version:{BroAudioData.CodeBaseVersion} ?", "Yes", "No"))
            {
                AssetDatabase.ExportPackage(allfilePaths.ToArray(), "BroAudio" + DateTime.Now.ToString("yy-MM-dd-HH-mm") + ".unitypackage", ExportPackageOptions.Interactive);
            }
        }
	}
}
#endif
