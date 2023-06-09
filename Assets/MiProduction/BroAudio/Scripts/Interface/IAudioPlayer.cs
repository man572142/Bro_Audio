using System;
using UnityEngine.Audio;

namespace MiProduction.BroAudio
{
	public interface IAudioPlayer
	{
		public int ID { get; }
		public bool IsPlaying { get; }

		public IAudioPlayer SetVolume(float vol, float fadeTime = 0.5f);
		public IAudioPlayer DuckOthers(float othersVol, float fadeTime = 0.5f);
		public IAudioPlayer LowPassOthers(float freq, float fadeTime = 0.5f);
		public IAudioPlayer HighPassOthers(float freq, float fadeTime = 0.5f);

		public void Stop();
		public void Stop(float fadeOut);
		public void Stop(Action onFinishStopping);
		public void Stop(float fadeOut, Action onFinishStopping);
	}
}