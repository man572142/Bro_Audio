using System;
using UnityEditor;

namespace Ami.Extension
{
    // Don't use UnityEditor.AnimValueBase<T>, it's non-linear and is hard-coded
    public abstract class EditorUpdateHelper : IDisposable
	{
		public event Action OnUpdate;

		private double _lastUpdateTime = default;
		protected float DeltaTime;
		protected abstract float UpdateInterval { get;}

		public virtual void Start()
		{
			EditorApplication.update -= UpdateInternal;
			EditorApplication.update += UpdateInternal;

			_lastUpdateTime = EditorApplication.timeSinceStartup;
		}

		public virtual void End()
		{
			EditorApplication.update -= UpdateInternal;
		}

		protected virtual void Update()
		{
            OnUpdate?.Invoke();
        }

		private void UpdateInternal()
		{
			double currentTime = EditorApplication.timeSinceStartup;
			if (currentTime - _lastUpdateTime >= UpdateInterval)
			{
                DeltaTime = (float)(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;
				Update();
			}
		}

        public virtual void Dispose()
        {
            EditorApplication.update -= UpdateInternal;
            OnUpdate = null;
        }
    }
}