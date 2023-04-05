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
            // 因為PlayOneShot沒辦法停，因此這裡是把音軌Mute掉
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

            // TODO: 播完馬上回收，多了pool的Add跟Remove好像有點浪費，看來還是得找個地方Cache
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