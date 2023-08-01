using System;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
    public interface IPlaybackControllable
    {
        void Play(int id, BroAudioClip clip, PlaybackPreference pref,bool waitForChainingMethod = true);

        void Stop();
		void Stop(float fadeOut);
		void Stop(Action onFinishStopping);
		void Stop(float fadeOut, Action onFinished);
        void Stop(float fadeOut, StopMode stopMode, Action onFinished);
    }
}