using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio.Core
{
    public class SoundPlayer : AudioPlayer
    {
        private const float DefaultFadeOutTime = 1f;

        private Coroutine _stopCoroutine;
        private Coroutine _recycleCoroutine;

        private void Start()
        {
            ClipVolume = 1f;
        }

		public void Play(int id, BroAudioClip clip, float delay, float preventTime)
		{
            ID = id;
			_stopCoroutine.StopIn(this);
			StartCoroutine(PlayOnce(clip, delay, preventTime));
		}

		public void PlayAtPoint(int id,BroAudioClip clip, float delay, Vector3 pos)
		{
            ID = id;
			_stopCoroutine.StopIn(this);
			StartCoroutine(PlayInScene(clip, delay, pos));
		}

		public override void Stop(float fadeOutTime)
        {
            // �]��PlayOneShot�S��k���A�]���o�̬O�⭵�yMute��
            if(fadeOutTime < 0)
			{
                fadeOutTime = DefaultFadeOutTime;
			}
            _stopCoroutine = StartCoroutine(Fade(fadeOutTime,0f));
        }


        private IEnumerator PlayOnce(BroAudioClip clip, float delay, float preventTime)
        {
            _recycleCoroutine.StopIn(this);

            yield return new WaitForSeconds(delay);

            ClipVolume = clip.Volume;
            IsPlaying = true;
            AudioSource.PlayOneShot(clip.AudioClip);

            yield return new WaitForSeconds(clip.AudioClip.length);
            IsPlaying = false;

            // TODO: �������W�^���A�h�Fpool��Add��Remove�n�����I���O�A�ݨ��٬O�o��Ӧa��Cache
            Recycle();
        }

        private IEnumerator PlayInScene(BroAudioClip clip, float delay, Vector3 pos)
        {
            yield return new WaitForSeconds(delay);
            IsPlaying = true;
            ClipVolume = clip.Volume;
            AudioSource.PlayClipAtPoint(clip.AudioClip, pos);
            yield return new WaitForSeconds(clip.AudioClip.length);
            IsPlaying = false;
        }
    }
}