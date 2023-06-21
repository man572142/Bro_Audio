using UnityEngine;
using MiProduction.BroAudio.Runtime;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio
{ 
    public static class BroAudio
    {
        #region Play
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="id"></param>
        public static IAudioPlayer Play(AudioID id) 
            => SoundManager.Instance.Play(id, AudioExtension.HaasEffectInSeconds);

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="id"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static IAudioPlayer Play(AudioID id, float preventTime) 
            => SoundManager.Instance.Play(id, preventTime);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position">播放的座標</param>
        //public static IAudioPlayer Play(AudioID id, Vector3 position)
        //  => SoundManager.Instance.PlayAtPoint(id, position,AudioExtension.HaasEffectInSeconds);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position">播放的座標</param>
        //public static IAudioPlayer Play(AudioID id, Vector3 position,float preventTime)
        //  => SoundManager.Instance.PlayAtPoint(id, position, preventTime);
        #endregion

        #region Stop
        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="audioType">停止的聲音類型</param>
        public static void Stop(BroAudioType audioType) 
            => SoundManager.Instance.StopPlaying(audioType);

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="fadeTime">自定FadeOut時間長度</param>
        /// <param name="id">停止的聲音ID (相同聲音類型的也都會停止)</param>
        public static void Stop(AudioID id) 
            => SoundManager.Instance.StopPlaying(id);
        #endregion

        #region Volume
        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        public static void SetVolume(float vol)
            => SetVolume(vol, BroAudioType.All);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, BroAudioType type) 
            => SetVolume(vol,type, BroAdvice.FadeTime_Quick);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="fadeTime">過渡到指定音量的時間</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, BroAudioType audioType, float fadeTime) 
            => SoundManager.Instance.SetVolume(vol, audioType, fadeTime);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="id">指定的AudioID</param>
        public static void SetVolume(float vol, int id) 
            => SetVolume(vol,id, BroAdvice.FadeTime_Quick);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="id">指定的AudioID</param>
        /// <param name="fadeTime">過渡到指定音量的時間</param>
        public static void SetVolume(float vol, int id, float fadeTime) 
            => SoundManager.Instance.SetVolume(vol, id, fadeTime);
        #endregion

        public static IAutoResetWaitable SetEffect(EffectParameter effect) 
            => SoundManager.Instance.SetEffect(effect);

        public static IAutoResetWaitable SetEffect(EffectParameter effect, BroAudioType audioType)
            => SoundManager.Instance.SetEffect(audioType,effect);
    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git
