using System;
using System.Threading.Tasks;
using System.Threading;

namespace Ami.Extension
{
	public static class AsyncTaskExtension
	{
		public const int SecondInMilliseconds = 1000;
		public const float MillisecondInSeconds = 0.001f;

		public static void DelayInvoke(float delay, Action action, CancellationToken cancellationToken = default)
		{
			float ms = delay * SecondInMilliseconds;
			// Do not remove the casting; otherwise, it will call to itself, creating an infinite loop
			DelayInvoke((int)ms, action, cancellationToken);
		}

		public static async void DelayInvoke(int milliseconds, Action action, CancellationToken cancellationToken = default)
		{
			try
			{
				await Task.Delay(milliseconds, cancellationToken);
				action.Invoke();
			}
			catch (TaskCanceledException)
			{
				// This exception is thrown by the task cancellation design, not an actual error.
			}
		}
	}
}