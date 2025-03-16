using UnityEngine;

namespace Ami.Extension
{
    public abstract class InstanceWrapper<T> : IRecyclable<InstanceWrapper<T>> where T : UnityEngine.Object
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
            _instance = newInstance;
        }

        public virtual void Recycle()
        {
            _instance = null;
        }

        protected virtual void LogInstanceIsNull()
        {
            Debug.LogError(BroAudio.Utility.LogTitle +  "The object that you are refering to is null.");
        }
    }
}