using System;

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

        IAudioPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade)
        {
            _transition = transition;
            _stopMode = stopMode;
            _overrideFade = overrideFade;
            return this;
        }

        public void DoTransition(ref PlaybackPreference pref)
        {
            // No BGM is playing
            if (CurrentBGMPlayer == null)
            {
                CurrentBGMPlayer = Instance;
                return;
            }

            HandleCurrentBGM();
            HandleNewBGM(ref pref);
            
            void HandleCurrentBGM()
            {
                IsWaitingForTransition = (_transition == Transition.Default || _transition == Transition.OnlyFadeOut) &&
                                         CurrentBGMPlayer.IsPlaying;
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
