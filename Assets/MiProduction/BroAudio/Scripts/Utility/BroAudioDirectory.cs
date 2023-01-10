using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public struct BroAudioDirectory
	{
		public string Path;
		public string FileName;

		public BroAudioDirectory(string path)
		{
			Path = path;
			FileName = null;
		}

		public BroAudioDirectory(string path, string fileName)
		{
			Path = path;
			FileName = fileName;
		}

		public BroAudioDirectory(BroAudioDirectory dir, string fileName)
		{
			Path = dir.Path;
			FileName = fileName;
		}
		public string LocalPath => Application.dataPath.Replace("Assets", string.Empty) + Path;

		public string FilePath => Path + "/" + FileName;
		public string LocalFilePath => LocalPath + "/" + FileName;
	} 
}