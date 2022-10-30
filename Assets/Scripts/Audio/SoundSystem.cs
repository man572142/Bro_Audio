using UnityEngine;
using MiProduction.BroAudio.Core;
using System;

namespace MiProduction.BroAudio
{ 
    public static class SoundSystem
    {
        #region SFX
        // *�ϥ�TEnum�|�Ψ�Boxing/Unboxing�A���ǷL���į�}�P(���D�`�p)

        /// <summary>
        /// ���񭵮� (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        public static void PlaySFX<TEnum>(TEnum sound) where TEnum : Enum
        {
            PlaySFX(sound, 0.1f);
        }

        /// <summary>
        /// ���񭵮� (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlaySFX<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            PlaySFX((int)(CoreLibraryEnum)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// ������������w�a�I���� (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="position">���񪺮y��</param>
        public static void PlaySFX<TEnum>(TEnum sound, Vector3 position) where TEnum : Enum
        {
            PlaySFX((int)(CoreLibraryEnum)(ValueType)sound, position);
        }

        public static void PlaySFX(int soundID) => SoundManager.Instance.PlaySFX(soundID, 0.1f);

        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlaySFX(int soundID, float preventTime) => SoundManager.Instance.PlaySFX(soundID, preventTime);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="position">���񪺮y��</param>
        public static void PlaySFX(int soundID, Vector3 position) => SoundManager.Instance.PlaySFX(soundID, position);
        #endregion

        #region RandomSFX
        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="sound"></param>
        public static void PlayRandomSFX<TEnum>(TEnum sound) where TEnum : Enum
        {
            PlayRandomSFX(sound,0.1f);
        }

        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlayRandomSFX<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            PlayRandomSFX((int)(CoreLibraryEnum)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip
        /// </summary>
        /// <param name="soundID"></param>
        public static void PlayRandomSFX(int soundID) => PlayRandomSFX(soundID, 0.1f);

        /// <summary>
        /// �H��������w���Ķ��X�������@��Clip
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static void PlayRandomSFX(int soundID, float preventTime) => SoundManager.Instance.PlayRandomSFX(soundID, preventTime);
        #endregion

        #region Music

        /// <summary>
        /// ���񭵼�(�ߧY����P����) (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        public static void PlayMusic<TEnum>(TEnum music) where TEnum : Enum
        {
            PlayMusic(music, Transition.Immediate);
        }

        /// <summary>
        /// ���񭵼� (�|�ϥΨ�Boxing/Unboxing�A��ĳ�i�H�NEnum�ରint����ID�ϥΡA�`�٤@�I�į�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static void PlayMusic<TEnum>(TEnum music, Transition transition, float fadeTime = -1f) where TEnum : Enum
        {
            PlayMusic((int)(CoreLibraryEnum)(ValueType)music, transition, fadeTime);
        }

        /// <summary>
        /// ���񭵼�(�ߧY����P����)
        /// </summary>
        /// <param name="musicID"></param>
        public static void PlayMusic(int musicID) => PlayMusic(musicID, Transition.Immediate);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="musicID">�i�����NEnum�ରID</param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static void PlayMusic(int musicID, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(musicID, transition, fadeTime);
        #endregion

        #region Stop
        /// <summary>
        /// �����(�ϥιw�]FadeOut�]�w)
        /// </summary>
        public static void Stop(AudioType type) => Stop(-1f, type);

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="fadeOutTime">�۩wFadeOut�ɶ�����</param>
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
