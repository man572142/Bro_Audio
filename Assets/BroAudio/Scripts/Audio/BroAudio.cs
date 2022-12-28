using UnityEngine;
using MiProduction.BroAudio.Core;
using System;

namespace MiProduction.BroAudio
{ 
    public static class BroAudio
    {
        #region SFX
        // *使用TEnum會用到Boxing/Unboxing，有些微的效能開銷(但非常小)

        /// <summary>
        /// 播放音效 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound) where TEnum : Enum
        {
            return PlaySound(sound, 0.1f);
        }

        /// <summary>
        /// 播放音效 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            return PlaySound((int)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// 於場景中的指定地點播放 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="position">播放的座標</param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound, Vector3 position) where TEnum : Enum
        {
            return PlaySound((int)(ValueType)sound, position);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundID"></param>
        public static IAudioPlayer PlaySound(int soundID) => SoundManager.Instance.PlaySound(soundID, 0.1f);

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static IAudioPlayer PlaySound(int soundID, float preventTime) => SoundManager.Instance.PlaySound(soundID, preventTime);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="position">播放的座標</param>
        public static IAudioPlayer PlaySound(int soundID, Vector3 position) => SoundManager.Instance.PlaySound(soundID, position);
        #endregion

        #region Music

        /// <summary>
        /// 播放音樂(立即播放與停止) (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        public static IAudioPlayer PlayMusic<TEnum>(TEnum music) where TEnum : Enum
        {
            return PlayMusic(music, Transition.Immediate);
        }

        /// <summary>
        /// 播放音樂 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static IAudioPlayer PlayMusic<TEnum>(TEnum music, Transition transition, float fadeTime = -1f) where TEnum : Enum
        {
            return PlayMusic((int)(ValueType)music, transition, fadeTime);
        }

        /// <summary>
        /// 播放音樂(立即播放與停止)
        /// </summary>
        /// <param name="musicID"></param>
        public static IAudioPlayer PlayMusic(int musicID) => PlayMusic(musicID, Transition.Immediate);

        /// <summary>
        /// 播放音樂
        /// </summary>
        /// <param name="musicID">可直接將Enum轉為ID</param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static IAudioPlayer PlayMusic(int musicID, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(musicID, transition, fadeTime);
        #endregion

        #region Stop
        /// <summary>
        /// 停止播放(使用預設FadeOut設定)
        /// </summary>
        public static void Stop(AudioType audioType) => Stop(-1f, audioType);

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="fadeOutTime">自定FadeOut時間長度</param>
        public static void Stop(float fadeTime, AudioType audioType) => SoundManager.Instance.StopPlaying(fadeTime, audioType);

        #endregion

        #region Volume
        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">過渡到指定音量的時間</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, AudioType type) => SetVolume(vol, 1f, type);

        /// <summary>
        /// 設定音量
        /// </summary>
        /// <param name="vol">0~1的音量值</param>
        /// <param name="fadeTime">過渡到指定音量的時間</param>
        /// <param name="type">設定的聲音類型</param>
        public static void SetVolume(float vol, float fadeTime, AudioType audioType) => SoundManager.Instance.SetVolume(vol, fadeTime, audioType);
        
        #endregion

    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git
