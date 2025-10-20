using System;
using System.Collections;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
	public interface IAutoResetWaitable
	{
		WaitUntil Until(Func<bool> predicate);
		WaitWhile While(Func<bool> condition);
		WaitForSeconds ForSeconds(float seconds);
	}
}