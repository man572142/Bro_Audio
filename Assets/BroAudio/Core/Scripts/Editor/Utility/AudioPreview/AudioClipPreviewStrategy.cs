using System;
using System.Reflection;
using System.Threading.Tasks;
using Ami.Extension;
using UnityEngine;
using static Ami.Extension.Reflection.ClassReflectionHelper;
using static Ami.Extension.TimeExtension;

namespace Ami.BroAudio.Editor
{
    public class AudioClipPreviewStrategy
    {
        public const string PlayClipMethodName = "PlayPreviewClip";
        public const string StopClipMethodName = "StopAllPreviewClips";

        public delegate void PlayPreviewClip(AudioClip audioClip, int startSample, bool loop);
        public delegate void StopAllPreviewClips();

        private IEditorPreviewModule _mainModule;

        private readonly StopAllPreviewClips _stopAllPreviewClipsDelegate = null;
        private readonly PlayPreviewClip _playPreviewClipDelegate = null;

        public AudioClipPreviewStrategy(IEditorPreviewModule mainModule)
        {
            _mainModule = mainModule;

            _stopAllPreviewClipsDelegate = GetAudioUtilMethodDelegate<StopAllPreviewClips>(StopClipMethodName);
            _playPreviewClipDelegate = GetAudioUtilMethodDelegate<PlayPreviewClip>(PlayClipMethodName);
        }

        public void Stop()
        {
            _mainModule.CancelTask();
            _stopAllPreviewClipsDelegate.Invoke();
            _mainModule.PlaybackIndicator.End();
            _mainModule.TriggerOnFinished();
        }

        public async Task PlayAsync(AudioClip audioClip, int startSample, int endSample, bool loop)
        {
            _playPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            _mainModule.PlaybackIndicator.Start(loop);

            int sampleLength = audioClip.samples - startSample - endSample;
            int lengthInMs = (int)Math.Round(sampleLength / (double)audioClip.frequency * SecondInMilliseconds, MidpointRounding.AwayFromZero);

            await Task.Delay(lengthInMs, _mainModule.CancellationSource.Token);

            if (loop)
            {
                while (loop)
                {
                    await AudioClipReplay(audioClip, startSample, loop, lengthInMs);
                }
            }
            else
            {
                Stop();
            }
        }

        private async Task AudioClipReplay(AudioClip audioClip, int startSample, bool loop, int lengthInMs)
        {
            _stopAllPreviewClipsDelegate.Invoke();
            _mainModule.PlaybackIndicator.End();

            _playPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            _mainModule.PlaybackIndicator.Start();

            await Task.Delay(lengthInMs, _mainModule.CancellationSource.Token);
        }

        private T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
        {
            Type audioUtilClass = GetUnityEditorClass(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            return Delegate.CreateDelegate(typeof(T), method) as T;
        }
    }
}