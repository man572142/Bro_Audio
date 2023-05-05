using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class AsyncTaskExtension
{
	public const int SecondInMilliseconds = 1000;
    public static async void DelayDoAction(float delay,Action action)
	{
		float ms = delay * SecondInMilliseconds;
		await Task.Delay((int)ms);
		action.Invoke();
	}
}
