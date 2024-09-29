using System;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public static class Empty
    {
        public static EmptyAudioPlayer AudioPlayer = new EmptyAudioPlayer();
        public static EmptyMusicPlayer MusicPlayer = new EmptyMusicPlayer();

        public static EmptyAudioSourceProxy AudioSource = new EmptyAudioSourceProxy();

        public class EmptyAudioPlayer : IAudioPlayer
        {
            SoundID IAudioPlayer.ID => SoundID.Invalid;
            bool IAudioPlayer.IsActive => false;
            bool IAudioPlayer.IsPlaying => false;
            IAudioSourceProxy IAudioPlayer.AudioSource => throw new NotImplementedException();

            event Action<SoundID> IAudioPlayer.OnEndPlaying
            {
                add { }
                remove { }
            }

            IMusicPlayer IMusicDecoratable.AsBGM() => MusicPlayer;
#if !UNITY_WEBGL
            IPlayerEffect IEffectDecoratable.AsDominator() => DominatorPlayer; 
#endif

            void IAudioPlayer.GetOutputData(float[] samples, int channels) { }
            void IAudioPlayer.GetSpectrumData(float[] samples, int channels, FFTWindow window) { }
            IAudioPlayer IAudioPlayer.OnAudioFilterRead(Action<float[], int> onAudioFilterRead) => this;
            IAudioPlayer IAudioPlayer.OnEnd(Action<SoundID> onEnd) => this;
            IAudioPlayer IAudioPlayer.OnStart(Action<IAudioPlayer> onStart) => this;
            IAudioPlayer IAudioPlayer.OnUpdate(Action<IAudioPlayer> onUpdate) => this;
            void IAudioStoppable.Pause() { }
            void IAudioStoppable.Pause(float fadeOut) { }
            void IAudioStoppable.UnPause() { }
            void IAudioStoppable.UnPause(float fadeOut) { }
            IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime) => this;
            IAudioPlayer IAudioPlayer.SetVelocity(int velocity) => this;
            IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => this;
            void IAudioStoppable.Stop() { }
            void IAudioStoppable.Stop(Action onFinished) { }
            void IAudioStoppable.Stop(float fadeOut) { }
            void IAudioStoppable.Stop(float fadeOut, Action onFinished) { }
        }

        public class EmptyMusicPlayer : EmptyAudioPlayer, IMusicPlayer
        {
            SoundID IMusicPlayer.ID => SoundID.Invalid;

            IAudioPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade) => AudioPlayer;
        }

#if !UNITY_WEBGL
        public static EmptyDominator DominatorPlayer = new EmptyDominator();

        public class EmptyDominator : EmptyAudioPlayer, IPlayerEffect
        {
            IPlayerEffect IPlayerEffect.HighPassOthers(float freq, float fadeTime) => this;
            IPlayerEffect IPlayerEffect.HighPassOthers(float freq, Fading fading) => this;
            IPlayerEffect IPlayerEffect.LowPassOthers(float freq, float fadeTime) => this;
            IPlayerEffect IPlayerEffect.LowPassOthers(float freq, Fading fading) => this;
            IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime) => this;
            IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, Fading fading) => this;
        } 
#endif
    }
}