using UnityEngine;
using MiProduction.BroAudio.Core;


namespace MiProduction.BroAudio
{

    public static class SoundManager 
    {
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sound"></param>
        public static void PlaySFX(Sound sound) => SoundSystem.Instance.PlaySFX(sound);

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlaySFX(Sound sound, float preventTime) => SoundSystem.Instance.PlaySFX(sound, preventTime);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position"></param>
        public static void PlaySFX(Sound sound, Vector3 position) => SoundSystem.Instance.PlaySFX(sound, position);


        public static void PlayRandomSFX(Sound sound) => SoundSystem.Instance.PlayRandomSFX(sound);

        public static void PlayRandomSFX(Sound sound, float preventTime) => SoundSystem.Instance.PlayRandomSFX(sound, preventTime);

        /// <summary>
        /// 播放音樂(立即播放與停止)
        /// </summary>
        /// <param name="newMusic"></param>
        public static void PlayMusic(Music newMusic) => SoundSystem.Instance.PlayMusic(newMusic);

        /// <summary>
        /// 播放音樂
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f) => SoundSystem.Instance.PlayMusic(newMusic, transition, fadeTime);

        /// <summary>
        /// 取得目前可用的Music Player
        /// </summary>
        /// <param name="musicPlayer"></param>
        /// <returns></returns>
        public static bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer) => SoundSystem.Instance.GetAvailableMusicPlayer(out musicPlayer);
    }

}



