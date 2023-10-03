using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Ami.Extension
{
	public abstract class EditorUpdateHelper
	{
		private double _lastUpdateTime = default;

		protected readonly float UpdateInterval = default;
		protected abstract void Update();

		public EditorUpdateHelper(float updateInterval)
		{
			UpdateInterval = updateInterval;
		}

		public virtual void Start()
		{
			EditorApplication.update -= InternalUpdate;
			EditorApplication.update += InternalUpdate;

			_lastUpdateTime = EditorApplication.timeSinceStartup;
		}

		public virtual void End()
		{
			EditorApplication.update -= InternalUpdate;
		}

		private void InternalUpdate()
		{
			double currentTime = EditorApplication.timeSinceStartup;
			if (currentTime - _lastUpdateTime >= UpdateInterval)
			{
				_lastUpdateTime = currentTime;

				Update();
			}
		}
	}
}