using UnityEngine;
using MiProduction.BroAudio.Core;


namespace MiProduction.BroAudio
{ 
    public static class SoundSystem
    {
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sound"></param>
        public static void PlaySFX(Sound sound) => SoundManager.Instance.PlaySFX(sound);

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlaySFX(Sound sound, float preventTime) => SoundManager.Instance.PlaySFX(sound, preventTime);

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position">播放的座標</param>
        public static void PlaySFX(Sound sound, Vector3 position) => SoundManager.Instance.PlaySFX(sound, position);

        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip
        /// </summary>
        /// <param name="sound"></param>
        public static void PlayRandomSFX(Sound sound) => SoundManager.Instance.PlayRandomSFX(sound);

        /// <summary>
        /// 隨機播放指定音效集合中的任一個Clip
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public static void PlayRandomSFX(Sound sound, float preventTime) => SoundManager.Instance.PlayRandomSFX(sound, preventTime);

        /// <summary>
        /// 播放音樂(立即播放與停止)
        /// </summary>
        /// <param name="newMusic"></param>
        public static void PlayMusic(Music newMusic) => SoundManager.Instance.PlayMusic(newMusic);

        /// <summary>
        /// 播放音樂
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public static void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(newMusic, transition, fadeTime);

        //public static bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer) => SoundManager.Instance.GetAvailableMusicPlayer(out musicPlayer);

        //public static void StopMusic() => SoundManager.Instance.StopMusic(-1f);

        
        //public static void StopMusic(float fadeOutTime) => SoundManager.Instance.StopMusic(fadeOutTime);


        //public static void StopMusicImmediately() => SoundManager.Instance.StopMusic(0f);

        /// <summary>
        /// 停止播放(使用預設FadeOut設定)
        /// </summary>
        public static void Stop(AudioType type) => Stop(-1f, type);

        /// <summary>
        /// 停止播放
        /// </summary>
        /// <param name="fadeOutTime">自定FadeOut時間長度</param>
        public static void Stop(float fadeTime,AudioType type)
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
        public static void SetVolume(float vol, float fadeTime,AudioType type)
		{
			switch (type)
			{
                case AudioType.All:
                    SoundManager.Instance.SetSFXVolume(vol, fadeTime);
                    SoundManager.Instance.SetMusicVolume(vol, fadeTime);
                    break;
				case AudioType.SFX:
                    SoundManager.Instance.SetSFXVolume(vol,fadeTime);
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

		//public static void SetVolumeExcept(float vol, float fadeTime, AudioType type)
	}
}
