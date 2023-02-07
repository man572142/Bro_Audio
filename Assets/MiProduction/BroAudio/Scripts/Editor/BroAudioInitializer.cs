using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio
{
	[InitializeOnLoad]
	public class BroAudioInitializer
	{
		private const string _coreDataSearchPattern = "MiProduction/BroAudio/BroAudioData.json";
		static BroAudioInitializer()
		{
			string[] coreDataFiles = Directory.GetFiles(Application.dataPath, _coreDataSearchPattern);
			if(coreDataFiles.Length > 1)
			{
				LogError("There is more than one BroAudioData.json, please delete duplicate files!");
			}
			else if(coreDataFiles.Length == 0)
			{
				LogError("Can't find the core file [BroAudioData.json],please relocate it!");
				BroAudioEditorWindow.ShowWindow();
			}

			string coreDataFilePath = coreDataFiles[0];
			string json = File.ReadAllText(coreDataFilePath);

			
			if (string.IsNullOrEmpty(json))
			{
				LogError("The core file [BroAudioData.json] is empty , please reimport or reinstall it!");
			}
			else
			{
				SerializedCoreData data = JsonUtility.FromJson<SerializedCoreData>(json);

				if (!string.IsNullOrEmpty(data.RootPath))
				{
					RootPath = data.RootPath;
				}
				if(!string.IsNullOrEmpty(data.EnumsPath))
				{
					EnumsPath = data.EnumsPath;
				}
			}
		}
	}

}