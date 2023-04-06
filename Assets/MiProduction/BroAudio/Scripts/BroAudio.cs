using UnityEngine;
using MiProduction.BroAudio.Core;
using System;

namespace MiProduction.BroAudio
{ 
    public static class BroAudio
    {
        #region SFX
        // *�ϥ�TEnum�|�Ψ�Boxing/Unboxing�A���ǷL���į�}�P

        /// <summary>
        /// ���񭵮� (��ĳ�i�NEnum�ରint����ID�ϥΡA�`�ٮį�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound) where TEnum : Enum
        {
            return PlaySound(sound, 0.1f);
        }

        /// <summary>
        /// ���񭵮� (��ĳ�i�NEnum�ରint����ID�ϥΡA�`�ٮį�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound, float preventTime) where TEnum : Enum
        {
            return PlaySound((int)(ValueType)sound, preventTime);
        }

        /// <summary>
        /// ������������w�a�I���� (��ĳ�i�NEnum�ରint����ID�ϥΡA�`�ٮį�}�P)
        /// </summary>
        /// <typeparam name="TEnum">���󭵮Ī�Enum</typeparam>
        /// <param name="sound"></param>
        /// <param name="position">���񪺮y��</param>
        public static IAudioPlayer PlaySound<TEnum>(TEnum sound, Vector3 position) where TEnum : Enum
        {
            return PlaySound((int)(ValueType)sound, position);
        }

        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="soundID"></param>
        public static IAudioPlayer PlaySound(int soundID) => SoundManager.Instance.PlaySound(soundID, 0.1f);

        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static IAudioPlayer PlaySound(int soundID, float preventTime) => SoundManager.Instance.PlaySound(soundID, preventTime);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="soundID"></param>
        /// <param name="position">���񪺮y��</param>
        public static IAudioPlayer PlaySound(int soundID, Vector3 position) => SoundManager.Instance.PlaySound(soundID, position);
        #endregion

        #region Music

        /// <summary>
        /// ���񭵼� (��ĳ�i�NEnum�ରint����ID�ϥΡA�`�ٮį�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        public static IAudioPlayer PlayMusic<TEnum>(TEnum music) where TEnum : Enum
        {
            return PlayMusic(music, Transition.Immediate);
        }

        /// <summary>
        /// ���񭵼� (��ĳ�i�NEnum�ରint����ID�ϥΡA�`�ٮį�}�P)
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="music"></param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static IAudioPlayer PlayMusic<TEnum>(TEnum music, Transition transition, float fadeTime = -1f) where TEnum : Enum
        {
            return PlayMusic((int)(ValueType)music, transition, fadeTime);
        }

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="musicID"></param>
        public static IAudioPlayer PlayMusic(int musicID) => PlayMusic(musicID, Transition.Immediate);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="musicID">�i�����NEnum�ରID</param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public static IAudioPlayer PlayMusic(int musicID, Transition transition, float fadeTime = -1f) => SoundManager.Instance.PlayMusic(musicID, transition, fadeTime);
        #endregion

        #region Stop

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="audioType">����n������</param>
        public static void Stop(AudioType audioType) => Stop(-1f, audioType);

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="id">����n��ID (�ۦP�n���������]���|����)</param>
        public static void Stop(int id) => Stop(-1f, id);

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="fadeTime">�۩wFadeOut�ɶ�����</param>
        /// <param name="audioType">����n������</param>
        public static void Stop(float fadeTime, AudioType audioType) => SoundManager.Instance.StopPlaying(fadeTime, audioType);

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="fadeTime">�۩wFadeOut�ɶ�����</param>
        /// <param name="id">����n��ID (�ۦP�n���������]���|����)</param>
        public static void Stop(float fadeTime, int id) => SoundManager.Instance.StopPlaying(fadeTime, id);

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
        /// <param name="vol">�L�����w���q���ɶ�</param>
        /// <param name="id">�]�w���n��ID (�ۦP�n�����������|�Q�]�w)</param>
        public static void SetVolume(float vol, int id) => SetVolume(vol, 1f, id);

        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">0~1�����q��</param>
        /// <param name="fadeTime">�L�����w���q���ɶ�</param>
        /// <param name="type">�]�w���n������</param>
        public static void SetVolume(float vol, float fadeTime, AudioType audioType) => SoundManager.Instance.SetVolume(vol, fadeTime, audioType);

        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">0~1�����q��</param>
        /// <param name="fadeTime">�L�����w���q���ɶ�</param>
        /// <param name="id">�]�w���n��ID (�ۦP�n�����������|�Q�]�w)</param>
        public static void SetVolume(float vol, float fadeTime, int id) => SoundManager.Instance.SetVolume(vol, fadeTime, id);

        #endregion

    }
}

// by �} 2022
// https://github.com/man572142/Bro_Audio.git
