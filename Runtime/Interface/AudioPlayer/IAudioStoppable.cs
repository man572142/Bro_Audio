using System;

namespace Ami.BroAudio
{
    public interface IAudioStoppable
    {
        internal void Stop();
        internal void Stop(Action onFinished);
        internal void Stop(float fadeOut);
        internal void Stop(float fadeOut, Action onFinished);
        internal void Pause();
        internal void Pause(float fadeTime);
        internal void UnPause();
        internal void UnPause(float fadeTime);
    }
}