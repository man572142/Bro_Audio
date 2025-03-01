﻿using UnityEngine;
using Ami.BroAudio.Runtime;
using System;

#if PACKAGE_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif 

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
        /// Plays audio globally (2D)
        /// </summary>
        public static IAudioPlayer Play(SoundID id) 
            => Play(id, (IPlayableValidator)null);

        ///<inheritdoc cref="Play(SoundID)"/>
        public static IAudioPlayer Play(SoundID id, IPlayableValidator customValidator)
            => SoundManager.Instance.Play(id, customValidator);

        /// <summary>
        /// Plays audio in 3D space at the given position
        /// </summary>
        public static IAudioPlayer Play(SoundID id, Vector3 position)
            => Play(id, position, null);

        ///<inheritdoc cref="Play(SoundID, Vector3)"/>
        public static IAudioPlayer Play(SoundID id, Vector3 position, IPlayableValidator customValidator)
            => SoundManager.Instance.Play(id, position, customValidator);

        /// <summary>
        /// Plays audio in 3D space and keeps it following the target continuously
        /// </summary>
        public static IAudioPlayer Play(SoundID id, Transform followTarget)
            => SoundManager.Instance.Play(id, followTarget, null);

        ///<inheritdoc cref="Play(SoundID, Transform)"/>
        public static IAudioPlayer Play(SoundID id, Transform followTarget, IPlayableValidator customValidator)
            => SoundManager.Instance.Play(id, followTarget, customValidator);
        #endregion

        #region Stop
        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        public static void Stop(BroAudioType audioType) 
            => SoundManager.Instance.Stop(audioType);

        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(BroAudioType audioType, float fadeOut)
            => SoundManager.Instance.Stop(audioType, fadeOut);

        /// <summary>
        /// Stop playing audio
        /// </summary>
        public static void Stop(SoundID id) 
            => SoundManager.Instance.Stop(id);

        /// <summary>
        /// Stop playing audio
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(SoundID id, float fadeOut)
            => SoundManager.Instance.Stop(id,fadeOut);
        #endregion

        #region Pause
        /// <summary>
        /// Pause audio
        /// </summary>
        public static void Pause(SoundID id) 
            => SoundManager.Instance.Pause(id, true);

        /// <summary>
        /// Pause audio
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Pause(SoundID id, float fadeOut)
            => SoundManager.Instance.Pause(id, fadeOut, true);

        /// <summary>
        /// Resume paused audio
        /// </summary>
        public static void UnPause(SoundID id)
            => SoundManager.Instance.Pause(id, false);

        /// <summary>
        /// Resume paused audio
        /// </summary>
        /// <param name="fadeIn">Set this value to override the LibraryManager's setting</param>
        public static void UnPause(SoundID id, float fadeIn)
            => SoundManager.Instance.Pause(id, fadeIn, false);

        /// <summary>
        /// Pause all audio that matches the given audio type
        /// </summary>
        public static void Pause(BroAudioType audioType)
            => SoundManager.Instance.Pause(audioType, true);

        /// <summary>
        /// Pause all audio that matches the given audio type
        /// </summary>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Pause(BroAudioType audioType, float fadeOut)
            => SoundManager.Instance.Pause(audioType, fadeOut, true);

        /// <summary>
        /// Resume all audio that matches the given audio type
        /// </summary>
        public static void UnPause(BroAudioType audioType)
            => SoundManager.Instance.Pause(audioType, false);

        /// <summary>
        /// Resume all audio that matches the given audio type
        /// </summary>
        /// <param name="fadeIn">Set this value to override the LibraryManager's setting</param>
        public static void UnPause(BroAudioType audioType, float fadeIn)
            => SoundManager.Instance.Pause(audioType, fadeIn, false);

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
            => SoundManager.Instance.SetVolume(vol, audioType, fadeTime);

        /// <summary>
        /// Set the audio volume
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        public static void SetVolume(SoundID id, float vol) 
            => SetVolume(id, vol, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the audio volume
        /// </summary>
        /// <param name="vol">Accepts values from 0 to 10, default is 1</param>
        /// <param name="fadeTime">Set this value to override the LibraryManager's setting</param>
        public static void SetVolume(SoundID id, float vol, float fadeTime) 
            => SoundManager.Instance.SetVolume(id, vol, fadeTime);
        #endregion

        #region Pitch
        /// <summary>
        /// Set the pitch of all audio immediately
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch)
            => SoundManager.Instance.SetPitch(pitch, BroAudioType.All, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Deprecated: The argument order is changed, please use <see cref="SetPitch(BroAudioType, float)"></see> instead
        /// </summary>
        [Obsolete]
        public static void SetPitch(float pitch, BroAudioType audioType)
            => SoundManager.Instance.SetPitch(pitch, audioType, BroAdvice.FadeTime_Immediate);


        /// <summary>
        /// Set the pitch of the given audio type immediately
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(BroAudioType audioType, float pitch)
            => SoundManager.Instance.SetPitch(pitch, audioType, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the pitch of all audio
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(float pitch, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, BroAudioType.All, fadeTime);

        /// <summary>
        /// Deprecated: The argument order is changed, please use <see cref="SetPitch(BroAudioType, float, float)"></see> instead
        /// </summary>
        [Obsolete]
        public static void SetPitch(float pitch, BroAudioType audioType, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, audioType, fadeTime);

        /// <summary>
        /// Set the pitch of the given audio type
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(BroAudioType audioType, float pitch, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, audioType, fadeTime);
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
        /// Set effect for all audio that matches the given audio type
        /// </summary>
        public static IAutoResetWaitable SetEffect(Effect effect, BroAudioType audioType)
            => SoundManager.Instance.SetEffect(audioType,effect);
        #endregion
#endif

#if PACKAGE_ADDRESSABLES
        /// <summary>
        /// Loads all the audio clips in the entity
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id) 
            => SoundManager.Instance.LoadAllAssetsAsync(id);

        /// <summary>
        /// Loads the first audio clip in the entity
        /// </summary>
        public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id)
            => LoadAssetAsync(id, 0);

        /// <summary>
        /// Loads the audio clip in the entity's clip list by index
        /// </summary>
        public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex)
            => SoundManager.Instance.LoadAssetAsync(id, clipIndex);

        /// <summary>
        /// Releases all the audio clips in the entity
        /// </summary>
        /// <param name="id"></param>
        public static void ReleaseAllAssets(SoundID id) 
            => SoundManager.Instance.ReleaseAllAssets(id);

        /// <summary>
        /// Releases the first audio clip in the entity
        /// </summary>
        /// <param name="id"></param>
        public static void ReleaseAsset(SoundID id)
            => SoundManager.Instance.ReleaseAsset(id, 0);

        /// <summary>
        /// Releases the audio clip in the entity's clip list by index
        /// </summary>
        /// <param name="id"></param>
        /// <param name="clipIndex"></param>
        public static void ReleaseAsset(SoundID id, int clipIndex)
            => SoundManager.Instance.ReleaseAsset(id, clipIndex);
#endif
    }
}
