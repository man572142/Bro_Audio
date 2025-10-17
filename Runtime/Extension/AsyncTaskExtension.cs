using System;
using System.Threading.Tasks;
using System.Threading;

namespace Ami.Extension
{
	public static class AsyncTaskExtension
	{
		public const float MillisecondInSeconds = 0.001f;

		public static async Task DelaySeconds(float seconds, CancellationToken cancellationToken = default)
		{
			await Delay(TimeExtension.SecToMs(seconds), cancellationToken);
		}

        public static async Task DelaySeconds(double seconds, CancellationToken cancellationToken = default)
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