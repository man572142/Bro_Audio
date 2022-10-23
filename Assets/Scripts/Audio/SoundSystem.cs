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
        //public static bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer) => SoundManager.Instance.GetAvailableMusicPlayer(out musicPlayer);

        /// <summary>
        /// ����񭵼�(�ϥιw�]FadeOut�]�w)
        /// </summary>
        public static void StopMusic() => SoundManager.Instance.StopMusic(-1f);

        /// <summary>
        /// ����񭵼�
        /// </summary>
        /// <param name="fadeOutTime">�۩wFadeOut�ɶ�����</param>
        public static void StopMusic(float fadeOutTime) => SoundManager.Instance.StopMusic(fadeOutTime);

        
        //public static void StopMusicImmediately() => SoundManager.Instance.StopMusic(0f);



        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">�L�����w���q���ɶ�</param>
        /// <param name="type">�]�w���n������</param>
        public static void SetVolume(float vol, AudioType type) => SetVolume(vol, 1f, type);

        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">0~1�����q��</param>
        /// <param name="fadeTime">�L�����w���q���ɶ�</param>
        /// <param name="type">�]�w���n������</param>
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
