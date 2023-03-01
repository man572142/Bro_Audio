using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Library.Core
{
	public enum LibraryState
	{
		Fine,
		HasEmptyName,
		HasNameDuplicated,
		HasInvalidName,
		NeedToUpdate,
	}

}