using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.Runtime
{
    public class OneShotPlayer : AudioPlayer
    {
        protected override void Start()
        {
            ClipVolume = 1f;
            base.Start();
        }

		public void PlayOneShot(int id, BroAudioClip clip, float delay)
		{
            ID = id;
            CurrentClip = clip;

            this.StartCoroutineAndReassign(PlayOneShot(clip, delay),ref PlaybackControlCoroutine);
		}

		public void PlayAtPoint(int id,BroAudioClip clip, float delay, Vector3 pos)
		{
            ID = id;
            CurrentClip = clip;
            this.StartCoroutineAndReassign(PlayInScene(clip, delay, pos), ref PlaybackControlCoroutine);

        }

		public override void Stop()
        {
            // 因為PlayOneShot沒辦法停，因此這裡是把音軌Mute掉
            this.StartCoroutineAndReassign(Fade(0f, CurrentClip.FadeOut, VolumeControl.Clip), ref PlaybackControlCoroutine);
        }


        private IEnumerator PlayOneShot(BroAudioClip clip, float delay)
        {
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
            // TODO: 把Player移到目標地點，設定pan，以發揮AudioPlayerObjectPool的作用
            AudioSource.PlayClipAtPoint(clip.AudioClip, pos);
            yield return new WaitForSeconds(clip.AudioClip.length);
            IsPlaying = false;
            Recycle();
        }
    }
}