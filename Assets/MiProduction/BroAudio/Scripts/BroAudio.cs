using UnityEngine;
using MiProduction.BroAudio.Runtime;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio
{ 
    public static class BroAudio
    {
        // TODO : 把這個刪掉
        public const string ProjectName = nameof(BroAudio);

        #region SFX
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

        //#region Music
        ///// <summary>
        ///// 播放音樂
        ///// </summary>
        ///// <param name="id"></param>
        //public static IAudioPlayer PlayMusic(AudioID id) 
        //    => PlayMusic(id, Transition.Default);

        ///// <summary>
        ///// 播放音樂
        ///// </summary>
        ///// <param name="musicID">可直接將Enum轉為ID</param>
        ///// <param name="transition">音樂過渡類型</param>
        //public static IAudioPlayer PlayMusic(AudioID id, Transition transition) 
        //    => PlayMusic(id, transition, AudioPlayer.UseClipFadeSetting);

        ///// <summary>
        ///// 播放音樂
        ///// </summary>
        ///// <param name="id">可直接將Enum轉為ID</param>
        ///// <param name="transition">音樂過渡類型</param>
        ///// <param name="fadeTime">限制該時間內不能再播放</param>
        //public static IAudioPlayer PlayMusic(AudioID id, Transition transition, float fadeTime) 
        //    => SoundManager.Instance.PlayMusic(id, transition,fadeTime,AudioExtension.HaasEffectInSeconds);
        //#endregion

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
        /// <param name="vol">過渡到指定音量的時間</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, BroAudioType type) => SetVolume(vol,type, 1f);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="fadeTime">過渡到指定音量的時間</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, BroAudioType audioType, float fadeTime) 
            => SoundManager.Instance.SetVolume(vol, fadeTime, audioType);
        #endregion
    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git
