using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio
{
	[InitializeOnLoad]
	public class CoreDataLocater
	{
		private const string _coreDataSearchPattern = "MiProduction/BroAudio/BroAudioData.json";
		static CoreDataLocater()
		{
			string[] coreDataFiles = Directory.GetFiles(Application.dataPath, _coreDataSearchPattern);
			if(coreDataFiles.Length > 1)
			{
				LogError("There is more than one BroAudioData.json, please delete duplicate files!");
			}
			else if(coreDataFiles.Length == 0)
			{
				LogError("Can't find the core file [BroAudioData.json],please relocate or reinstall BroAudio!");
				BroAudioEditorWindow.ShowWindow();
			}

			string coreDataFilePath = coreDataFiles[0];
			string json = File.ReadAllText(coreDataFilePath);

			SerializedCoreData data = default;
			if (string.IsNullOrEmpty(json))
			{
				// 與其寫到Json 還不如直接改這個Script的const ?
				int assetsPathLength = UnityAssetsRootPath.Length + 1;  // with one slash
				int rootPathLength = coreDataFilePath.Length - assetsPathLength - CoreDataFileName.Length;
				RootPath = coreDataFilePath.Substring(assetsPathLength, rootPathLength).Replace(CoreDataFileName, string.Empty);
				data.RootPath = RootPath;
				json = JsonUtility.ToJson(data);
				File.WriteAllText(coreDataFilePath, json);
			}
			else
			{
				data = JsonUtility.FromJson<SerializedCoreData>(json);

				if (!string.IsNullOrEmpty(data.RootPath))
				{
					RootPath = data.RootPath;
				}
			}
			Debug.Log($"RootPath:{RootPath}");
		}
	}

}