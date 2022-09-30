using UnityEngine;
using MiProduction.BroAudio.Core;


namespace MiProduction.BroAudio
{ 
    public static class SoundSystem
    {
        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="sound"></param>
        public static void PlaySFX(Sound sound) => SoundManager.Instance.PlaySFX(sound);

        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlaySFX(Sound sound, float preventTime) => SoundManager.Instance.PlaySFX(sound, preventTime);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position">���񪺮y��</param>
        public static void PlaySFX(Sound sound, Vector3 position) => SoundManager.Instance.PlaySFX(sound, position);

        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip
        /// </summary>
        /// <param name="sound"></param>
        public static void PlayRandomSFX(Sound sound) => SoundManager.Instance.PlayRandomSFX(sound);

        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlayRandomSFX(Sound sound, float preventTime) => SoundManager.Instance.PlayRandomSFX(sound, preventTime);

        /// <summary>
        /// ���񭵼�(�ߧY����P����)
        /// </summary>
        /// <param name="newMusic"></param>
        public static void PlayMusic(Music newMusic) => SoundManager.Instance.PlayMusic(newMusic);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(newMusic, transition, fadeTime);

        /// <summary>
        /// ���o�ثe�i�Ϊ�Music Player
        /// </summary>
        /// <param name="musicPlayer"></param>
        /// <returns></returns>
        public static bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer) => SoundManager.Instance.GetAvailableMusicPlayer(out musicPlayer);

        /// <summary>
        /// ����񭵼�(�ϥιw�]FadeOut�]�w)
        /// </summary>
        public static void StopMusic() => SoundManager.Instance.StopMusic(-1f);

        /// <summary>
        /// ����񭵼�
        /// </summary>
        /// <param name="fadeTime">�۩wFadeOut�ɶ�����</param>
        public static void StopMusic(float fadeTime) => SoundManager.Instance.StopMusic(fadeTime);

        /// <summary>
        /// �ߧY����񭵼�
        /// </summary>
        public static void StopMusicImmediately() => SoundManager.Instance.StopMusic(0f);

        public static void SetMusicVolume(float vol) => SoundManager.Instance.SetMusicVolume(vol, 1f);

        public static void SetMusicVolume(float vol, float fadeTime) => SoundManager.Instance.SetMusicVolume(vol, fadeTime);
    }  
}
