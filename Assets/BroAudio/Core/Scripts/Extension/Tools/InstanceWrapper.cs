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
			UnityEngine.Debug.LogError(BroAudio.Utility.LogTitle +  "The object that you are refering to is null.");
		}
	}

}