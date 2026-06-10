using System;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class MusicPlayer : AudioPlayerDecorator, IMusicPlayer
    {
        internal static event Action<IAudioPlayer> OnBGMChanged;
        private static AudioPlayer _currentBGMPlayer = null;

        public static AudioPlayer CurrentBGMPlayer
        {
            get => _currentBGMPlayer;
            set
            {
                if (_currentBGMPlayer != value)
                {
                    _currentBGMPlayer = value;
                    var instance = value != null ? value.GetInstanceWrapper() : null;
                    OnBGMChanged?.Invoke(instance);
                }
            }
        }

        private Transition _transition = default;
        private StopMode _stopMode = default;
        private float _overrideFade = FadeData.UseClipSetting;

        public bool IsWaitingForTransition { get; private set; }
        public bool NeedTransition => CurrentBGMPlayer != null && CurrentBGMPlayer.IsActive && CurrentBGMPlayer != Instance;

        public MusicPlayer(AudioPlayer audioPlayer) : base(audioPlayer)
        {
        }

        public override void Recycle ()
        {
            if(CurrentBGMPlayer == Instance)
            {
                CurrentBGMPlayer = null;
            }
            base.Recycle();
            _transition = default;
            _stopMode = default;
            _overrideFade = FadeData.UseClipSetting;
        }

        public override void UpdateInstance(AudioPlayer newInstance)
        {
            // Track the live player across loop/chain iterations so transitions act on the playing source.
            // Write the backing field to skip OnBGMChanged — the logical BGM hasn't changed.
            if (_currentBGMPlayer == Instance)
            {
                _currentBGMPlayer = newInstance;
            }
            base.UpdateInstance(newInstance);
        }

        IAudioPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade)
        {
            _transition = transition;
            _stopMode = stopMode;
            _overrideFade = overrideFade;
            return this;
        }

        public void DoTransition(ref PlaybackPreference pref)
        {
            // No prior BGM, this player is already current (pause/resume or same-instance replay), or the prior player is gone — stopping it would self-stop mid-PlayControl or double-recycle it.
            if (!NeedTransition)
            {
                CurrentBGMPlayer = Instance;
                return;
            }

            HandleCurrentBGM();
            HandleNewBGM(ref pref);
            
            void HandleCurrentBGM()
            {
                bool fadeOutTransition = _transition == Transition.Default || _transition == Transition.OnlyFadeOut;
                IsWaitingForTransition = fadeOutTransition && CurrentBGMPlayer && CurrentBGMPlayer.IsActive;
                if (IsWaitingForTransition)
                {
                    StopCurrentPlayer(FinishTransition);
                }
                else
                {
                    StopCurrentPlayer();
                    CurrentBGMPlayer = Instance;
                }
            }

            void HandleNewBGM(ref PlaybackPreference pref)
            {
                float fadeIn = _transition switch
                {
                    Transition.Immediate => 0f,
                    Transition.OnlyFadeOut => 0f,
                    Transition.OnlyFadeIn => _overrideFade,
                    Transition.Default => _overrideFade,
                    Transition.CrossFade => _overrideFade,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                pref.SetNextFadeIn(fadeIn);
            }
        }

        private void FinishTransition()
        {
            IsWaitingForTransition = false;
            CurrentBGMPlayer = Instance;
        }

        private void StopCurrentPlayer(Action onFinished = null)
        {
            bool noFadeOut = _transition == Transition.Immediate || _transition == Transition.OnlyFadeIn;
            float fadeOut =  noFadeOut? 0f : _overrideFade;
            CurrentBGMPlayer.Stop(fadeOut, _stopMode, onFinished);
        }

        public static void CleanUp()
        {
            OnBGMChanged = null;
            _currentBGMPlayer = null;
        }
    }
}
