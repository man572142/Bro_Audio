using System;

namespace Ami.BroAudio
{
    public interface IAudioStoppable
    {
        void Stop();
        void Stop(Action onFinished);
        void Stop(float fadeOut);
        void Stop(float fadeOut, Action onFinished);
        void Pause();
        void Pause(float fadeOut);
    }

}