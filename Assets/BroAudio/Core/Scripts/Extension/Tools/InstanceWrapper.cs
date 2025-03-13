using System;
using UnityEngine;

namespace Ami.Extension
{
	public abstract class InstanceWrapper<T> where T : UnityEngine.Object, IRecyclable<T>
	{
		private T _instance = null;
		protected T Instance
		{
			get
			{
                if (IsAvailable())
                {
                    return _instance;
                }
				return null;
            }
		}

        protected InstanceWrapper(T instance)
		{
            _instance = instance;
			_instance.OnRecycle += Recycle;
		}

        protected bool IsAvailable(bool logWarning = true)
        {
            if(_instance != null)
            {
                return true;
            }

            if(logWarning)
            {
                LogInstanceIsNull();
            }
            return false;
        }

        public virtual void UpdateInstance(T newInstance)
        {
            ClearEvent();
            _instance = newInstance;
            _instance.OnRecycle += Recycle;
        }

        protected virtual void Recycle(T t)
        {
            ClearEvent();
            _instance = null;
        }

		private void ClearEvent()
		{
            if (_instance)
            {
                _instance.OnRecycle -= Recycle;
            }
        }

		protected virtual void LogInstanceIsNull()
		{
			Debug.LogError(BroAudio.Utility.LogTitle +  "The object that you are refering to is null.");
		}
	}
}