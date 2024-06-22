using System;
using System.Threading.Tasks;
using System.Threading;

namespace Ami.Extension
{
	public static class AsyncTaskExtension
	{
		public const float MillisecondInSeconds = 0.001f;

		public static void DelayInvoke(float delay, Action action, CancellationToken cancellationToken = default)
		{
			int ms = TimeExtension.SecToMs(delay);
			// Do not remove the casting; otherwise, it will call to itself, creating an infinite loop
			DelayInvoke(ms, action, cancellationToken);
		}

		public static async void DelayInvoke(int milliseconds, Action action, CancellationToken cancellationToken = default)
		{
			if(milliseconds <= 0)
			{
				return;
			}

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

		public static async Task DelaySeconds(float seconds, CancellationToken cancellationToken = default)
		{
			await Delay(TimeExtension.SecToMs(seconds), cancellationToken);
		}

		public static async Task Delay(int milliseconds, CancellationToken cancellationToken = default)
		{
            try
            {
                await Task.Delay(milliseconds, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // This exception is thrown by the task cancellation design, not an actual error.
            }
        }

		public static bool IsCanceled(this CancellationTokenSource source)
		{
			return source == null || source.IsCancellationRequested;
		}
	}
}