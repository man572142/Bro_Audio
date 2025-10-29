using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Data;
using System;

#if PACKAGE_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif 

namespace Ami.BroAudio
{
    public static class BroAudio
    {
#if BroAudio_InitManually
        public static void Init()
        {
            SoundManager.Init();
        }
#endif
        
        public static event Action<IAudioPlayer> OnBGMChanged
        {
            add => MusicPlayer.OnBGMChanged += value;
            remove => MusicPlayer.OnBGMChanged -= value;
        }

        #region Play
        /// <summary>
        /// Plays audio globally (2D)
        /// </summary>
        /// <param name="playableValidator">Overrides the validation rule of the <see cref="PlaybackGroup"/></param>
        public static IAudioPlayer Play(SoundID id, IPlayableValidator playableValidator = null)
            => Play(id, FadeData.UseClipSetting, playableValidator);

        /// <param name="fadeIn">Time in seconds to fade in at start</param>
        /// <inheritdoc cref="Play(SoundID, IPlayableValidator)"/>
        public static IAudioPlayer Play(SoundID id, float fadeIn, IPlayableValidator playableValidator = null)
            => SoundManager.Instance.Play(id, fadeIn, playableValidator);

        /// <summary>
        /// Plays audio in 3D space at the given position
        /// </summary>
        /// <param name="playableValidator">Overrides the validation rule of the <see cref="PlaybackGroup"/></param>
        public static IAudioPlayer Play(SoundID id, Vector3 position, IPlayableValidator playableValidator = null)
            => Play(id, position, FadeData.UseClipSetting, playableValidator);
        
        /// <param name="fadeIn">Time in seconds to fade in at start</param>
        /// <inheritdoc cref="Play(SoundID, Vector3, IPlayableValidator)"/>
        public static IAudioPlayer Play(SoundID id, Vector3 position, float fadeIn, IPlayableValidator playableValidator = null)
            => SoundManager.Instance.Play(id, position, fadeIn, playableValidator);

        /// <summary>
        /// Plays audio in 3D space and keeps it following the target continuously
        /// </summary>
        /// <param name="playableValidator">Overrides the validation rule of the <see cref="PlaybackGroup"/></param>
        public static IAudioPlayer Play(SoundID id, Transform followTarget, IPlayableValidator playableValidator = null)
            => Play(id, followTarget, FadeData.UseClipSetting, playableValidator);
        
        /// <param name="fadeIn">Time in seconds to fade in at start</param>
        /// <inheritdoc cref="Play(SoundID, Transform, IPlayableValidator)"/>
        public static IAudioPlayer Play(SoundID id, Transform followTarget, float fadeIn, IPlayableValidator playableValidator = null)
            => SoundManager.Instance.Play(id, followTarget, fadeIn, playableValidator);
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
        /// <param name="fadeOut">Duration in seconds to fade out. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeOut">Duration in seconds to fade out. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeOut">Duration in seconds to fade out. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeIn">Duration in seconds to fade in. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeOut">Duration in seconds to fade out. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeOut">Duration in seconds to fade in. Set this value to override the LibraryManager's setting</param>
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
        /// <param name="fadeTime">Duration in seconds to fade from current to target</param>
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
        /// <param name="fadeTime">Duration in seconds to fade from current to target</param>
        public static void SetVolume(SoundID id, float vol, float fadeTime) 
            => SoundManager.Instance.SetVolume(id, vol, fadeTime);
        #endregion

        #region Pitch

        /// <summary>
        /// Set the audio pitch immediately
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        public static void SetPitch(SoundID id, float pitch)
            => SoundManager.Instance.SetPitch(id, pitch, BroAdvice.FadeTime_Immediate);
        
        /// <summary>
        /// Set the audio pitch
        /// </summary>
        /// <param name="pitch">values between -3 to 3, default is 1</param>
        /// <param name="fadeTime">Duration in seconds to fade from current to target</param>
        public static void SetPitch(SoundID id, float pitch, float fadeTime)
            => SoundManager.Instance.SetPitch(id, pitch, fadeTime);
        
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
        /// <param name="fadeTime">Duration in seconds to fade from current to target</param>
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
        /// <param name="fadeTime">Duration in seconds to fade from current to target</param>
        public static void SetPitch(BroAudioType audioType, float pitch, float fadeTime)
            => SoundManager.Instance.SetPitch(pitch, audioType, fadeTime);
        #endregion

        #region Reset MultiClips Data
        public static void ResetMultiClipStrategy(SoundID id)
        {
            if (id.Entity != null)
            {
                id.Entity.ResetMultiClipStrategy();
            }
        }
        #endregion

        /// <summary>
        /// Checks if the sound is playing anywhere
        /// </summary>
        public static bool HasAnyPlayingInstances(SoundID id)
            => SoundManager.Instance.HasAnyPlayingInstances(id);

        /// <summary>
        /// Get the read-only information of an audio entity
        /// </summary>
        public static bool TryGetEntityInfo(SoundID id, out IReadOnlyAudioEntity entityInfo)
        {
            entityInfo = null;
            if (SoundManager.Instance != null && SoundManager.Instance.TryGetEntity(id, out var entity, false))
            {
                entityInfo = entity as IReadOnlyAudioEntity;
                return entityInfo != null;
            }
            return false;
        }

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
        public static bool IsLoaded(SoundID id)
            => SoundManager.Instance.IsLoaded(id);

        public static bool IsLoaded(SoundID id, int clipIndex)
            => SoundManager.Instance.IsLoaded(id, clipIndex);

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

#if UNITY_EDITOR
        private delegate bool TRYCONVERTID(int id, out AudioEntity entity);
        private static TRYCONVERTID _tryConvertIdBroAudioEditorUtility = null;
#endif

        [System.Obsolete("Only for backwards compatibility")]
        public static bool TryConvertIdToEntity(int id, out AudioEntity entity)
        {
            if (id == 0 || id == -1)
            {
                entity = null;
                return false;
            }

#if !UNITY_EDITOR // Can't call from serialization threads, and since we're in editor we can just use the much more capable converter in the editor tools
            if (SoundManager.Instance != null && SoundManager.Instance.TryConvertIdToEntity(id, out entity))
            {
                return true;
            }
#endif

#if UNITY_EDITOR
            try
            {
                _tryConvertIdBroAudioEditorUtility ??= (TRYCONVERTID)System.Type.GetType("Ami.BroAudio.Editor.BroEditorUtility, BroAudioEditor", true)
                    .GetMethod("TryConvertIdToEntity", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                    .CreateDelegate(typeof(TRYCONVERTID), null);

                return _tryConvertIdBroAudioEditorUtility(id, out entity);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
#endif

            Debug.LogError($"Could not find entity with ID {id} to convert SoundID to entity with");
            entity = null;
            return false;
        }
    }
}
