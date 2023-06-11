using UnityEngine;
using MiProduction.BroAudio.Runtime;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio
{ 
    public static class BroAudio
    {
        public const string ProjectName = nameof(BroAudio);

        #region SFX
        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="id"></param>
        public static IAudioPlayer Play(AudioID id) 
            => SoundManager.Instance.Play(id, AudioExtension.HaasEffectInSeconds);

        /// <summary>
        /// ���񭵮�
        /// </summary>
        /// <param name="id"></param>
        /// <param name="preventTime">����Ӯɶ�������A����</param>
        public static IAudioPlayer Play(AudioID id, float preventTime) 
            => SoundManager.Instance.Play(id, preventTime);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position">���񪺮y��</param>
        //public static IAudioPlayer Play(AudioID id, Vector3 position)
        //  => SoundManager.Instance.PlayAtPoint(id, position,AudioExtension.HaasEffectInSeconds);

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="id"></param>
        /// <param name="position">���񪺮y��</param>
        //public static IAudioPlayer Play(AudioID id, Vector3 position,float preventTime)
        //  => SoundManager.Instance.PlayAtPoint(id, position, preventTime);
        #endregion

        #region Music
        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="id"></param>
        public static IAudioPlayer PlayMusic(AudioID id) 
            => PlayMusic(id, Transition.Default);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="musicID">�i�����NEnum�ରID</param>
        /// <param name="transition">���ֹL������</param>
        public static IAudioPlayer PlayMusic(AudioID id, Transition transition) 
            => PlayMusic(id, transition, AudioPlayer.UseClipFadeSetting);

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="id">�i�����NEnum�ରID</param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">����Ӯɶ�������A����</param>
        public static IAudioPlayer PlayMusic(AudioID id, Transition transition, float fadeTime) 
            => SoundManager.Instance.PlayMusic(id, transition,fadeTime,AudioExtension.HaasEffectInSeconds);
        #endregion

        #region Stop
        /// <summary>
        /// �����
        /// </summary>
        /// <param name="audioType">����n������</param>
        public static void Stop(BroAudioType audioType) 
            => SoundManager.Instance.StopPlaying(audioType);

        /// <summary>
        /// �����
        /// </summary>
        /// <param name="fadeTime">�۩wFadeOut�ɶ�����</param>
        /// <param name="id">����n��ID (�ۦP�n���������]���|����)</param>
        public static void Stop(AudioID id) 
            => SoundManager.Instance.StopPlaying(id);
        #endregion

        #region Volume
        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">�L�����w���q���ɶ�</param>
        /// <param name="type">�]�w���n������</param>
        public static void SetVolume(float vol, BroAudioType type) => SetVolume(vol,type, 1f);

        /// <summary>
        /// �]�w���q
        /// </summary>
        /// <param name="vol">0~1�����q��</param>
        /// <param name="fadeTime">�L�����w���q���ɶ�</param>
        /// <param name="type">�]�w���n������</param>
        public static void SetVolume(float vol, BroAudioType audioType, float fadeTime) 
            => SoundManager.Instance.SetVolume(vol, fadeTime, audioType);
        #endregion
    }
}

// by �} 2022
// https://github.com/man572142/Bro_Audio.git
