using System;

namespace Ami.BroAudio
{
    public interface IAudioStoppable
    {
#if UNITY_2020_2_OR_NEWER
		internal void Stop();
		internal void Stop(Action onFinished);
		internal void Stop(float fadeOut);
		internal void Stop(float fadeOut, Action onFinished);
		internal void Pause();
		internal void Pause(float fadeOut);
#else
        void Stop();
        void Stop(Action onFinished);
        void Stop(float fadeOut);
        void Stop(float fadeOut, Action onFinished);
        void Pause();
        void Pause(float fadeOut);
#endif
	}
}