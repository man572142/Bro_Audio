using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Reflection;
using static Ami.Extension.Reflection.ClassReflectionHelper;
using static Ami.Extension.TimeExtension;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
    public class DirectPlaybackPreviewStrategy : EditorPreviewStrategy
    {
        public delegate void PlayPreviewClip(AudioClip audioClip, int startSample, bool loop);
        public delegate void StopAllPreviewClips();

        private const string PlayClipMethodName = "PlayPreviewClip";
        private const string StopClipMethodName = "StopAllPreviewClips";

        private readonly StopAllPreviewClips _stopAllPreviewClipsDelegate = GetAudioUtilMethodDelegate<StopAllPreviewClips>(StopClipMethodName);
        private readonly PlayPreviewClip _playPreviewClipDelegate = GetAudioUtilMethodDelegate<PlayPreviewClip>(PlayClipMethodName);

        public override async void Play(PreviewRequest request, ReplayRequest replayRequest = null)
        {
            if (request?.AudioClip == null)
            {
                return;
            }

            try
            {
                await PlayClipAsync(request);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipAsync(PreviewRequest request)
        {
            Stop();

            AudioClip audioClip = request.AudioClip;
            int startSample = audioClip.GetTimeSample(request.StartPosition);
            int endSample = request.EndPosition > 0 ? audioClip.GetTimeSample(request.EndPosition) : 0;

            _playPreviewClipDelegate.Invoke(audioClip, startSample, false);
            StartPlaybackIndicator();

            int sampleLength = audioClip.samples - startSample - endSample;
            int lengthInMs = (int)Math.Round(sampleLength / (double)audioClip.frequency * SecondInMilliseconds, MidpointRounding.AwayFromZero);

            await Task.Delay(lengthInMs, CancellationSource.Token);

            StopPlayback();
        }

        private void StopPlayback()
        {
            CancelTask();
            _stopAllPreviewClipsDelegate.Invoke();
            EndPlaybackIndicator();
            TriggerOnFinished();
        }

        private static T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
        {
            Type audioUtilClass = GetUnityEditorClass(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            return method != null ? Delegate.CreateDelegate(typeof(T), method) as T : null;
        }

        public override void Stop()
        {
            StopPlayback();
        }

        public override void Dispose()
        {
            base.Dispose();
            StopPlayback();
        }
    }
}
