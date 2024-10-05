using UnityEngine;
using Ami.BroAudio.Runtime;
using System;

namespace Ami.BroAudio
{
    public static class BroAudio
    {
        public static event Action<IAudioPlayer> OnBGMChanged
        {
            add => MusicPlayer.OnBGMChanged += value;
            remove => MusicPlayer.OnBGMChanged -= value;
        }

        #region Play
        /// <summary>
        /// Plays an audio
        /// </summary>
        public static IAudioPlayer Play(SoundID id) 
            => SoundManager.Instance.Play(id);

        /// <summary>
        /// Plays an audio as 3D sound at the given position
        /// </summary>
        public static IAudioPlayer Play(SoundID id, Vector3 position)
          => SoundManager.Instance.Play(id, position);

        /// <summary>
        /// Plays an audio as 3D sound and keeps it continuously following the target
        /// </summary>
        public static IAudioPlayer Play(SoundID id, Transform followTarget)
          => SoundManager.Instance.Play(id, followTarget);
        #endregion

        #region Stop
        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        public static void Stop(BroAudioType audioType) 
            => SoundManager.Instance.Stop(audioType.ConvertEverything());

        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(BroAudioType audioType,float fadeOut)
            => SoundManager.Instance.Stop(audioType.ConvertEverything(), fadeOut);

        /// <summary>
        /// Stop playing an audio
        /// </summary>
        public static void Stop(SoundID id) 
            => SoundManager.Instance.Stop(id);

        /// <summary>
        /// Stop playing an audio
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(SoundID id,float fadeOut)
            => SoundManager.Instance.Stop(id,fadeOut);
        #endregion

        #region Pause
        /// <summary>
        /// Pause a sound
        /// </summary>
        public static void Pause(SoundID id) 
            => SoundManager.Instance.Pause(id, true);

        /// <summary>
        /// Pause a sound
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Pause(SoundID id, float fadeOut)
            => SoundManager.Instance.Pause(id, true, fadeOut);

        /// <summary>
        /// Resume a paused sound
        /// </summary>
        public static void UnPause(SoundID id)
            => SoundManager.Instance.Pause(id, false);

        /// <summary>
        /// Resume a paused sound
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void UnPause(SoundID id, float fadeOut)
            => SoundManager.Instance.Pause(id, false, fadeOut);

        #endregion

        #region Volume
        /// <summary>
        /// Set the master volume
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        public static void SetVolume(float vol, float fadeTime = BroAdvice.FadeTime_Immediate)
            => SetVolume(BroAudioType.All, vol, fadeTime);

        /// <summary>
        /// Set the volume of the given audio type
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        /// <param name="fadeTime">Set this value to override the LibraryManager's setting</param>
        public static void SetVolume(BroAudioType audioType, float vol, float fadeTime = BroAdvice.FadeTime_Immediate) 
            => SoundManager.Instance.SetVolume(vol, audioType.ConvertEverything(), fadeTime);

        /// <summary>
        /// Set the volume of an audio
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        public static void SetVolume(SoundID id, float vol) 
            => SetVolume(id, vol, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the volume of an audio
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        /// <param name="fadeTime">Set this value to override the LibraryManager's setting</param>
        public static void SetVolume(SoundID id, float vol, float fadeTime) 
            => SoundManager.Instance.SetVolume(id, vol, fadeTime);
        #endregion

        #region Pitch
        /// <summary>
        /// Set all audio's pitch immediately
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch)
            => SoundManager.Instance.SetPitch(pitch, BroAudioType.All, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the pitch of the given audio type immediately
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch, BroAudioType audioType)
            => SoundManager.Instance.SetPitch(pitch, audioType.ConvertEverything(), BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set all audio's pitch
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, BroAudioType.All, fadeTime);

        /// <summary>
        /// Set the pitch of the given audio type
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch, BroAudioType audioType, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, audioType.ConvertEverything(), fadeTime);
        #endregion

        #region Reset MultiClips Data
        public static void ResetSequence(SoundID id)
        {
            Utility.ResetClipSequencer(id);
        }

        public static void ResetSequence()
        {
            Utility.ResetClipSequencer();
        }

        public static void ResetShuffle(SoundID id)
        {
            SoundManager.Instance.ResetShuffleInUseState(id);
        }

        public static void ResetShuffle()
        {
            SoundManager.Instance.ResetShuffleInUseState();
        } 
        #endregion

#if !UNITY_WEBGL
        #region Effect
        /// <summary>
        /// Set effect for all audio
        /// </summary>
        public static IAutoResetWaitable SetEffect(Effect effect) 
            => SoundManager.Instance.SetEffect(effect);

        /// <summary>
        /// Set effect for all audio that mathch the given audio type
        /// </summary>
        public static IAutoResetWaitable SetEffect(Effect effect, BroAudioType audioType)
            => SoundManager.Instance.SetEffect(audioType.ConvertEverything(),effect);
#endregion
#endif
	}
}
