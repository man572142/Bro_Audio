using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		private const string _defaultRootPath = "Assets/MiProduction/BroAudio";
		private const string _defaultEnumsPath = "Assets/MiProduction/BroAudio/Scripts/Enums";
		private const string _jsonFileName = "BroAudioData.json";

		public static BroAudioDirectory RootDir = new BroAudioDirectory(_defaultRootPath);
		public static BroAudioDirectory EnumsDir = new BroAudioDirectory(_defaultEnumsPath);
		public static BroAudioDirectory JsonFileDir = new BroAudioDirectory(_defaultRootPath, _jsonFileName);
	}

}