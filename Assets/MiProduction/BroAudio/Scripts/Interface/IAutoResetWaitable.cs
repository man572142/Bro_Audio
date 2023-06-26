using System;
using System.Collections;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
	public interface IAutoResetWaitable
	{
		WaitUntil Until(Func<bool> predicate);
		Coroutine Until(Coroutine coroutine);
		IEnumerator Until(IEnumerator enumerator);
		WaitWhile While(Func<bool> predicate);
		WaitForSeconds ForSeconds(float seconds);
	}
}