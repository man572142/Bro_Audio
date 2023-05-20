using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;
using System;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.Core
{
    public class OneShotPlayer : AudioPlayer
    {
        private const float DefaultFadeOutTime = 1f;

        private Coroutine _stopCoroutine;
        private Coroutine _recycleCoroutine;

        protected override void Start()
        {
            ClipVolume = 1f;
            base.Start();
        }

		public void PlayOneShot(int id, BroAudioClip clip, float delay)
		{
            ID = id;
			_stopCoroutine.StopIn(this);
			StartCoroutine(PlayOneShot(clip, delay));
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


        private IEnumerator PlayOneShot(BroAudioClip clip, float delay)
        {
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