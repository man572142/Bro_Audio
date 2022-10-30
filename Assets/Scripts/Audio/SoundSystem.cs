using UnityEngine;
using MiProduction.BroAudio.Core;
using System;

namespace MiProduction.BroAudio
{ 
    public static class SoundSystem
    {
        #region SFX
        // *使用TEnum會用到Boxing/Unboxing，有些微的效能開銷(但非常小)

        /// <summary>
        /// 播放音效 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        public static void PlaySFX<TEnum>(TEnum sound) where TEnum : Enum
        {
            PlaySFX(sound, 0.1f);
        }

        /// <summary>
        /// 播放音效 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlaySFX<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            PlaySFX((int)(CoreLibraryEnum)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// 於場景中的指定地點播放 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum">任何音效的Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="position">播放的座標</param>
        public static void PlaySFX<TEnum>(TEnum sound, Vector3 position) where TEnum : Enum
        {
            PlaySFX((int)(CoreLibraryEnum)(ValueType)sound, position);
        }

        public static void PlaySFX(int soundID) => SoundManager.Instance.PlaySFX(soundID, 0.1f);

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlaySFX(int soundID, float preventTime) => SoundManager.Instance.PlaySFX(soundID, preventTime);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="position">播放的座標</param>
        public static void PlaySFX(int soundID, Vector3 position) => SoundManager.Instance.PlaySFX(soundID, position);
        #endregion

        #region RandomSFX
        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="sound"></param>
        public static void PlayRandomSFX<TEnum>(TEnum sound) where TEnum : Enum
        {
            PlayRandomSFX(sound,0.1f);
        }

        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlayRandomSFX<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            PlayRandomSFX((int)(CoreLibraryEnum)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip
        /// </summary>
        /// <param name="soundID"></param>
        public static void PlayRandomSFX(int soundID) => PlayRandomSFX(soundID, 0.1f);

        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlayRandomSFX(int soundID, float preventTime) => SoundManager.Instance.PlayRandomSFX(soundID, preventTime);
        #endregion

        #region Music

        /// <summary>
        /// 播放音樂(立即播放與停止) (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        public static void PlayMusic<TEnum>(TEnum music) where TEnum : Enum
        {
            PlayMusic(music, Transition.Immediate);
        }

        /// <summary>
        /// 播放音樂 (會使用到Boxing/Unboxing，建議可以將Enum轉為int做為ID使用，節省一點效能開銷)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static void PlayMusic<TEnum>(TEnum music, Transition transition, float fadeTime = -1f) where TEnum : Enum
        {
            PlayMusic((int)(CoreLibraryEnum)(ValueType)music, transition, fadeTime);
        }

        /// <summary>
        /// 播放音樂(立即播放與停止)
        /// </summary>
        /// <param name="musicID"></param>
        public static void PlayMusic(int musicID) => PlayMusic(musicID, Transition.Immediate);

        /// <summary>
        /// 播放音樂
        /// </summary>
        /// <param name="musicID">可直接將Enum轉為ID</param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static void PlayMusic(int musicID, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(musicID, transition, fadeTime);
        #endregion

        #region Stop
        /// <summary>
        /// 停止播放(使用預設FadeOut設定)
        /// </summary>
        public static void Stop(AudioType type) => Stop(-1f, type);

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="fadeOutTime">自定FadeOut時間長度</param>
        public static void Stop(float fadeTime, AudioType type)
        {
            switch (type)
            {
                case AudioType.All:
                    SoundManager.Instance.StopMusic(fadeTime);
                    SoundManager.Instance.StopSFX(fadeTime);
                    break;
                case AudioType.SFX:
                    SoundManager.Instance.StopSFX(fadeTime);
                    break;
                case AudioType.Music:
                    SoundManager.Instance.StopMusic(fadeTime);
                    break;
                case AudioType.Ambience:
                    break;
                case AudioType.UI:
                    break;
            }
        } 
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
        public static void SetVolume(float vol, float fadeTime, AudioType type)
        {
            switch (type)
            {
                case AudioType.All:
                    SoundManager.Instance.SetSFXVolume(vol, fadeTime);
                    SoundManager.Instance.SetMusicVolume(vol, fadeTime);
                    break;
                case AudioType.SFX:
                    SoundManager.Instance.SetSFXVolume(vol, fadeTime);
                    break;
                case AudioType.Music:
                    SoundManager.Instance.SetMusicVolume(vol, fadeTime);
                    break;
                case AudioType.Ambience:
                    break;
                case AudioType.UI:
                    break;
            }
        } 
        #endregion

    }
}
