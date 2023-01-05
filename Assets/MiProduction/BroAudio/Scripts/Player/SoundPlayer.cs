using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Core
{
    public class SoundPlayer : AudioPlayer
    {
        private const float DefaultFadeOutTime = 1f;

        private Dictionary<int, bool> _preventPlayback = new Dictionary<int, bool>();
        private Coroutine _stopCoroutine;

        public override bool IsPlaying { get; protected set; }
        public override bool IsStoping { get; protected set; }
        public override bool IsFadingOut { get; protected set; }
        public override bool IsFadingIn { get; protected set; }

        private void Start()
        {
            ClipVolume = 1f;
        }

		public void Play(int id, BroAudioClip clip, float delay, float preventTime)
		{
			_stopCoroutine.Stop(this);
			StartCoroutine(PlayOnce(id, clip, delay, preventTime));
		}

		public void PlayAtPoint(BroAudioClip clip, float delay, Vector3 pos)
		{
			_stopCoroutine.Stop(this);
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


        private IEnumerator PlayOnce(int id, BroAudioClip clip, float delay, float preventTime)
        {
            yield return new WaitForSeconds(delay);
            if(_preventPlayback.TryGetValue(id,out bool isPreventing))
            {
                if(isPreventing)
                    yield break;
            }
            else if(preventTime > 0)
            {
                _preventPlayback.Add(id, true);
            }

            ClipVolume = clip.Volume;
            IsPlaying = true;
            AudioSource.PlayOneShot(clip.AudioClip);

            if (preventTime > 0)
                StartCoroutine(PreventPlaybackControl(id, preventTime));

            yield return new WaitForSeconds(clip.AudioClip.length);
            IsPlaying = false;
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


        IEnumerator PreventPlaybackControl(int id, float time)
        {
            _preventPlayback[id] = true;
            yield return new WaitForSeconds(time);
            _preventPlayback[id] = false;
        }
    }

}