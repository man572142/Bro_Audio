using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
	public struct AudioData
	{
		public int ID;
		public string Name;

		public AudioData(int iD, string name)
		{
			ID = iD;
			Name = name;
		}
	}

}