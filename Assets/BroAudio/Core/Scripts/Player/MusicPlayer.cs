using System;

namespace Ami.BroAudio.Runtime
{
    public class MusicPlayer : AudioPlayerDecorator, IMusicPlayer
    {
        internal static event Action<IAudioPlayer> OnBGMChanged;
        private static AudioPlayer _currentPlayer = null;

        public static AudioPlayer CurrentPlayer
        {
            get => _currentPlayer;
            set
            {
                if (_currentPlayer != value)
                {
                    _currentPlayer = value;
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
            if(CurrentPlayer == Instance)
            {
                CurrentPlayer = null;
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

        public void Transition(ref PlaybackPreference pref)
        {
            if (CurrentPlayer != null)
            {
                pref.SetFadeTime(_transition, _overrideFade);
                switch (_transition)
                {
                    case Ami.BroAudio.Transition.Immediate:
                    case Ami.BroAudio.Transition.OnlyFadeIn:
                    case Ami.BroAudio.Transition.CrossFade:
                        StopCurrentMusic();
                        break;
                    case Ami.BroAudio.Transition.Default:
                    case Ami.BroAudio.Transition.OnlyFadeOut:
                        if(CurrentPlayer.IsPlaying)
                        {
                            IsWaitingForTransition = true;
                            StopCurrentMusic(() =>
                            {
                                IsWaitingForTransition = false;
                                CurrentPlayer = Instance;
                            });
                            return;
                        }	
                        break;
                }
            }
            CurrentPlayer = Instance;
        }

        private void StopCurrentMusic(Action onFinished = null)
        {
            bool noFadeOut = _transition == Ami.BroAudio.Transition.Immediate || _transition == Ami.BroAudio.Transition.OnlyFadeIn;
            float fadeOut =  noFadeOut? 0f : _overrideFade;
            CurrentPlayer.Stop(fadeOut, _stopMode, onFinished);
        }

        public static void CleanUp()
        {
            OnBGMChanged = null;
            _currentPlayer = null;
        }
    }
}
