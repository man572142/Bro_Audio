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
        private float _overrideFade = AudioPlayer.UseEntitySetting;

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
            _overrideFade = AudioPlayer.UseEntitySetting;
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
                IsWaitingForTransition = _transition is Transition.Default or Transition.OnlyFadeOut &&
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
                pref.FadeIn = _transition switch
                {
                    Transition.Immediate or Transition.OnlyFadeOut => 0f,
                    Transition.OnlyFadeIn or Transition.Default or Transition.CrossFade => _overrideFade,
                    _ => pref.FadeIn
                };
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
