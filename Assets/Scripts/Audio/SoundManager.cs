using UnityEngine;
using MiProduction.BroAudio.Core;


namespace MiProduction.BroAudio
{

    public static class SoundManager 
    {
        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="sound"></param>
        public static void PlaySFX(Sound sound) => SoundSystem.Instance.PlaySFX(sound);

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlaySFX(Sound sound, float preventTime) => SoundSystem.Instance.PlaySFX(sound, preventTime);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position"></param>
        public static void PlaySFX(Sound sound, Vector3 position) => SoundSystem.Instance.PlaySFX(sound, position);


        public static void PlayRandomSFX(Sound sound) => SoundSystem.Instance.PlayRandomSFX(sound);

        public static void PlayRandomSFX(Sound sound, float preventTime) => SoundSystem.Instance.PlayRandomSFX(sound, preventTime);

        /// <summary>
        /// ���񭵼�(�ߧY����P����)
        /// </summary>
        /// <param name="newMusic"></param>
        public static void PlayMusic(Music newMusic) => SoundSystem.Instance.PlayMusic(newMusic);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f) => SoundSystem.Instance.PlayMusic(newMusic, transition, fadeTime);

        /// <summary>
        /// ���o�ثe�i�Ϊ�Music Player
        /// </summary>
        /// <param name="musicPlayer"></param>
        /// <returns></returns>
        public static bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer) => SoundSystem.Instance.GetAvailableMusicPlayer(out musicPlayer);
    }

}



