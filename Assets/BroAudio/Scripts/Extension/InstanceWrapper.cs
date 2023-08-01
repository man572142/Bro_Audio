using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
	public abstract class InstanceWrapper<T>
	{
		protected T Instance;

		protected InstanceWrapper(T instance)
		{
			Instance = instance;
		}

		protected virtual bool IsAvailable()
		{
			if (Instance != null)
			{
				return true;
			}
			else
			{
				LogInstanceIsNull();
				return false;
			}
		}

		protected virtual void LogInstanceIsNull()
		{
			Debug.LogError("The object that you are refering to is null.");
		}
	}

}