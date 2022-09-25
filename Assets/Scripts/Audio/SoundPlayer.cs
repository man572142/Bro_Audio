using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Core
{
    public class SoundPlayer : MonoBehaviour
    {
        private Dictionary<Sound, bool> _preventPlayback = new Dictionary<Sound, bool>();
        [SerializeField] private AudioSource _sfxPlayer = null;

        public void Play(Sound sound, AudioClip clip, float delay, float volume, float preventTime)
        {
            StartCoroutine(PlayOnce(sound, clip, delay, volume, preventTime));
        }

        public void PlayAtPoint(Sound sound, AudioClip clip, float delay, float volume, Vector3 pos)
        {
            StartCoroutine(PlayInScene(sound, clip, volume, delay, pos));
        }

        public void Stop()
        {

        }

        public void Mute()
        {

        }

        private IEnumerator PlayOnce(Sound sound, AudioClip clip, float delay, float volume, float preventTime)
        {
            yield return new WaitForSeconds(delay);
            if(_preventPlayback.TryGetValue(sound,out bool isPreventing))
            {
                if(isPreventing)
                    yield break;
            }
            else if(preventTime > 0)
            {
                _preventPlayback.Add(sound, true);
            }

            _sfxPlayer.PlayOneShot(clip, volume);
            if (preventTime > 0)
                StartCoroutine(PreventPlaybackControl(sound, preventTime));
        }

        private IEnumerator PlayInScene(Sound sound, AudioClip clip, float volume, float delay, Vector3 pos)
        {
            yield return new WaitForSeconds(delay);
            AudioSource.PlayClipAtPoint(clip, pos, volume);
            yield break;
        }


        IEnumerator PreventPlaybackControl(Sound sound, float time)
        {
            _preventPlayback[sound] = true;
            yield return new WaitForSeconds(time);
            _preventPlayback[sound] = false;
        }
    }

}