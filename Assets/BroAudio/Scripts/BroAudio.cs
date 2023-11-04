using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio
{ 
    public static class BroAudio
    {
        #region Play
        /// <summary>
        /// Play an audio
        /// </summary>
        /// <param name="id"></param>
        public static IAudioPlayer Play(AudioID id) 
            => SoundManager.Instance.Play(id);

        /// <summary>
        /// Play an audio at the given position
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position"></param>
        public static IAudioPlayer Play(AudioID id, Vector3 position)
          => SoundManager.Instance.Play(id, position);

        /// <summary>
        /// Play an audio and let it keep following the target
        /// </summary>
        /// <param name="id"></param>
        /// <param name="followTarget"></param>
        public static IAudioPlayer Play(AudioID id, Transform followTarget)
          => SoundManager.Instance.Play(id, followTarget);
        #endregion

        #region Stop
        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        /// <param name="audioType"></param>
        public static void Stop(BroAudioType audioType) 
            => SoundManager.Instance.Stop(audioType);

        /// <summary>
        /// Stop playing all audio that match the given audio type
        /// </summary>
        /// <param name="audioType"></param>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(BroAudioType audioType,float fadeOut)
            => SoundManager.Instance.Stop(audioType, fadeOut);

        /// <summary>
        /// Stop playing an audio
        /// </summary>
        /// <param name="id"></param>
        public static void Stop(AudioID id) 
            => SoundManager.Instance.Stop(id);

        /// <summary>
        /// Stop playing an audio
        /// </summary>
        /// <param name="id"></param>
        /// /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Stop(AudioID id,float fadeOut)
            => SoundManager.Instance.Stop(id,fadeOut);
        #endregion

        #region Pause
        /// <summary>
        /// Pause an audio
        /// </summary>
        /// <param name="id"></param>
        public static void Pause(AudioID id) 
            => SoundManager.Instance.Pause(id);

        /// <summary>
        /// Pause an audio
        /// </summary>
        /// <param name="id"></param>
        /// <param name="fadeOut">Set this value to override the LibraryManager's setting</param>
        public static void Pause(AudioID id, float fadeOut)
            => SoundManager.Instance.Pause(id,fadeOut);

        #endregion

        #region Volume
        /// <summary>
        /// Set the master volume
        /// </summary>
        /// <param name="vol">0.0 to 1.0</param>
        public static void SetVolume(float vol)
            => SetVolume(vol, BroAudioType.All);

        /// <summary>
        /// Set the volume of the given audio type
        /// </summary>
        /// <param name="vol">0.0 to 1.0</param>
        /// <param name="audioType"></param>
        public static void SetVolume(float vol, BroAudioType audioType) 
            => SetVolume(vol,audioType, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the volume of the given audio type
        /// </summary>
        /// <param name="vol">0.0 to 1.0</param>
        /// <param name="audioType"></param>
        /// <param name="fadeTime">Set this value to override the LibraryManager's setting</param>
        public static void SetVolume(float vol, BroAudioType audioType, float fadeTime) 
            => SoundManager.Instance.SetVolume(vol, audioType, fadeTime);

        /// <summary>
        /// Set the volume of an audio
        /// </summary>
        /// <param name="vol">0.0 to 1.0</param>
        /// <param name="id"></param>
        public static void SetVolume(AudioID id, float vol) 
            => SetVolume(id, vol, BroAdvice.FadeTime_Immediate);

        /// <summary>
        /// Set the volume of an audio
        /// </summary>
        /// <param name="vol">0.0 to 1.0</param>
        /// <param name="id"></param>
        /// <param name="fadeTime">Set this value to override the LibraryManager's setting</param>
        public static void SetVolume(AudioID id, float vol, float fadeTime) 
            => SoundManager.Instance.SetVolume(id, vol, fadeTime);
        #endregion

#if !UNITY_WEBGL
#region Effect
        /// <summary>
        /// Set effect for all audio
        /// </summary>
        /// <param name="effect"></param>
        /// <returns></returns>
        public static IAutoResetWaitable SetEffect(EffectParameter effect) 
            => SoundManager.Instance.SetEffect(effect);

        /// <summary>
        /// Set effect for all audio that mathch the given audio type
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="audioType"></param>
        /// <returns></returns>
        public static IAutoResetWaitable SetEffect(EffectParameter effect, BroAudioType audioType)
            => SoundManager.Instance.SetEffect(audioType,effect);
#endregion
#endif
	}
}
