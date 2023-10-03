using System;
using System.Threading.Tasks;


namespace Ami.Extension
{
	public static class AsyncTaskExtension
	{
		public const int SecondInMilliseconds = 1000;
		public const float MillisecondInSeconds = 0.001f;

		public static async void DelayDoAction(float delay, Action action)
		{
			float ms = delay * SecondInMilliseconds;
			await Task.Delay((int)ms);
			action.Invoke();
		}
	}
}