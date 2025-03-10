using Ami.BroAudio.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ami.BroAudio
{
    public partial class SoundSource : MonoBehaviour
    {
        #region Play
        /// <summary>
        /// Plays the audio base on the current PositionMode 
        /// </summary>
        public void Play()
        {
            switch (_positionMode)
            {
                case PositionMode.Global:
                    PlayGlobally();
                    break;
                case PositionMode.FollowGameObject:
                    Play(transform);
                    break;
                case PositionMode.StayHere:
                    Play(transform.position);
                    break;
            }
        }

        ///<inheritdoc cref="BroAudio.Play(SoundID)"/>
        public void PlayGlobally()
        {
            Stop();
            CurrentPlayer = BroAudio.Play(_sound, _overrideGroup);
        }

        ///<inheritdoc cref="BroAudio.Play(SoundID, Transform)"/>
        public void Play(Transform followTarget)
        {
            Stop();
            CurrentPlayer = BroAudio.Play(_sound, followTarget, _overrideGroup);
        }

        ///<inheritdoc cref="Play(SoundID, Vector3)"/>
        public void Play(Vector3 positon)
        {
            Stop();
            CurrentPlayer = BroAudio.Play(_sound, positon, _overrideGroup);
        }
        #endregion

        #region Stop and Pause
        ///<inheritdoc cref="BroAudio.Stop(SoundID)"/>
        public void Stop() => Stop(AudioPlayer.UseEntitySetting);

        ///<inheritdoc cref="BroAudio.Stop(SoundID, float)"/>
        public void Stop(float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.Stop(fadeTime);
            }
        }

        ///<inheritdoc cref="BroAudio.Pause(SoundID)"/>
        public void Pause() => Pause(AudioPlayer.UseEntitySetting);

        ///<inheritdoc cref="BroAudio.Pause(SoundID, float)"/>
        public void Pause(float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.Pause(fadeTime);
            }
        }

        ///<inheritdoc cref="BroAudio.UnPause(SoundID)"/>
        public void UnPause() => UnPause(AudioPlayer.UseEntitySetting);

        ///<inheritdoc cref="BroAudio.UnPause(SoundID, float)"/>
        public void UnPause(float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.UnPause(fadeTime);
            }
        }
        #endregion

        #region Modification
        ///<inheritdoc cref="BroAudio.SetVolume(SoundID,float)"/>
        public void SetVolume(float vol) => SetVolume(vol, BroAdvice.FadeTime_Immediate);

        ///<inheritdoc cref="BroAudio.SetVolume(SoundID,float,float)"/>
        public void SetVolume(float vol, float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.SetVolume(vol, fadeTime);
            }
        }

        public void SetPitch(float pitch) => SetPitch(pitch, BroAdvice.FadeTime_Immediate);
        public void SetPitch(float pitch, float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.SetPitch(pitch, fadeTime);
            }
        } 
        #endregion
    }
}
