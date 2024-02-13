using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
	public abstract class ObjectPool<T> where T : class
	{
		protected readonly int MaxPoolSize;

		protected T BaseObject = null;
		protected List<T> Pool = new List<T>();

		protected abstract T CreateObject();
		protected abstract void DestroyObject(T instance);


		protected ObjectPool(T baseObject, int maxInternalPoolSize)
		{
			BaseObject = baseObject;
			MaxPoolSize = maxInternalPoolSize;
		}

		public virtual void Init(int initialCount)
		{
			for (int i = 0; i < initialCount; i++)
			{
				T obj = CreateObject();
				Pool.Add(obj);
			}
		}

		public virtual T Extract()
		{
			T obj = null;
			if (Pool.Count == 0)
			{
				obj = CreateObject();
			}
			else
			{
				int lastIndex = Pool.Count - 1;
				obj = Pool[lastIndex];
				Pool.RemoveAt(lastIndex);
			}

			return obj;
		}

		public virtual void Recycle(T obj)
		{
			if (Pool.Count == MaxPoolSize)
			{
				DestroyObject(obj);
			}
			else
			{
				Pool.Add(obj);
			}
		}
	}
}
