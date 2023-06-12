using System;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
    public interface IPlaybackControllable
    {
        void Play(int id, BroAudioClip clip, PlaybackPreference pref);

        void Stop();
		void Stop(float fadeOut);
		void Stop(Action onFinishStopping);
		void Stop(float fadeOut, Action onFinished);
        void Stop(float fadeOut, StopMode stopMode, Action onFinished);
    }
}